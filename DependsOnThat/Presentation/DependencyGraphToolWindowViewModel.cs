#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using DependsOnThat.Collections;
using DependsOnThat.Disposables;
using DependsOnThat.Extensions;
using DependsOnThat.Graph;
using DependsOnThat.Graph.Display;
using DependsOnThat.Roslyn;
using DependsOnThat.Services;
using DependsOnThat.Statistics;
using DependsOnThat.Text;
using DependsOnThat.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Threading;
using QuickGraph;

namespace DependsOnThat.Presentation
{
	internal sealed class DependencyGraphToolWindowViewModel : ViewModelBase, IDisposable
	{
		private readonly IDocumentsService _documentsService;
		private readonly IRoslynService _roslynService;
		private readonly IGitService _gitService;
		private readonly ISolutionService _solutionService;
		private readonly IOutputService _outputService;
		private readonly IModificationsService _modificationsService;
		private readonly JoinableTaskFactory _joinableTaskFactory;

		private readonly HashSet<string> _rootDocuments = new HashSet<string>();

		private readonly GraphStateManager _graphStateManager;

		private readonly SerialCancellationDisposable _projectUpdatesRegistration = new SerialCancellationDisposable();
		private readonly SerialCancellationDisposable _statsRetrievalRegistration = new SerialCancellationDisposable();

		private bool _shouldUseGitForRoots;

		public IBidirectionalGraph<DisplayNode, DisplayEdge> Graph { get => _graph; set => OnValueSet(ref _graph, value); }

		private IBidirectionalGraph<DisplayNode, DisplayEdge> _graph = Empty;
		private static IBidirectionalGraph<DisplayNode, DisplayEdge> Empty { get; } = new BidirectionalGraph<DisplayNode, DisplayEdge>();

		public int ExtensionDepth
		{
			get => _graphStateManager.ExtensionDepth;
			set => OnValueSet(_graphStateManager.ExtensionDepth, v => _graphStateManager.ExtensionDepth = v, value);
		}

		public bool ExcludePureGenerated
		{
			get => _graphStateManager.ExcludePureGenerated;
			set => OnValueSet(_graphStateManager.ExcludePureGenerated, v => _graphStateManager.ExcludePureGenerated = v, value);
		}

		private DisplayNode? _selectedNode;
		public DisplayNode? SelectedNode
		{
			get => _selectedNode;
			set
			{
				if (OnValueSet(ref _selectedNode, value))
				{
					if (value?.FilePath != null
						// Don't try to open the currently-open file. (This can, eg, disrupt the 'diff' view being opened from the source control changes window.)
						&& value.FilePath != _documentsService.GetActiveDocument())
					{
						_joinableTaskFactory.RunAsync(async () =>
						{
							await Dispatcher.Yield(); // Opening a file takes a noticeable delay, so give the visuals a chance to update first
							if (SelectedNode == value)
							{
								_documentsService.OpenFileAsPreview(value.FilePath);
							}
						});
					}
				}
			}
		}

		private TimeSpan? _graphingtime;
		public TimeSpan? GraphingTime { get => _graphingtime; set => OnValueSet(ref _graphingtime, value); }

		private string? _graphingError;
		public string? GraphingError { get => _graphingError; set => OnValueSet(ref _graphingError, value); }

		private SelectionList<ProjectIdentifier>? _projects;
		public SelectionList<ProjectIdentifier>? Projects
		{
			get => _projects;
			set
			{
				var oldValue = _projects;
				if (OnValueSet(ref _projects, value))
				{
					if (oldValue != null)
					{
						oldValue.SelectionChanged -= OnProjectsSelectionChanged;
					}
					if (value != null)
					{
						value.SelectionChanged += OnProjectsSelectionChanged;
					}
				}
			}
		}

		private void OnProjectsSelectionChanged() => _graphStateManager.SetIncludedProjects(Projects?.SelectedItems);

		public ICommand AddActiveDocumentAsRootCommand { get; }
		public ICommand ClearRootsCommand { get; }
		public ICommand UseGitModifiedFilesAsRootCommand { get; }
		public ICommand LogStatsCommand { get; }

		public DependencyGraphToolWindowViewModel(JoinableTaskFactory joinableTaskFactory, IDocumentsService documentsService, IRoslynService roslynService, IGitService gitService, ISolutionService solutionService, IOutputService outputService, IModificationsService modificationsService)
		{
			_joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
			_documentsService = documentsService ?? throw new ArgumentNullException(nameof(documentsService));
			_roslynService = roslynService ?? throw new ArgumentNullException(nameof(roslynService));
			_gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
			_solutionService = solutionService ?? throw new ArgumentNullException(nameof(solutionService));
			_outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));
			_modificationsService = modificationsService ?? throw new ArgumentNullException(nameof(modificationsService));

			_documentsService.ActiveDocumentChanged += OnActiveDocumentChanged;
			_solutionService.SolutionChanged += OnSolutionChanged;
			_modificationsService.DocumentInvalidated += OnDocumentInvalidated;
			_modificationsService.SolutionInvalidated += OnSolutionChanged;

			AddActiveDocumentAsRootCommand = SimpleCommand.Create(AddActiveDocumentAsRoot);
			ClearRootsCommand = SimpleCommand.Create(ClearRoots);
			UseGitModifiedFilesAsRootCommand = SimpleCommand.Create(UseGitModifiedFilesAsRoot);
			LogStatsCommand = SimpleCommand.Create(LogStats);

			_graphStateManager = new GraphStateManager(joinableTaskFactory, () => _roslynService.GetCurrentSolution(), GetCurrentRootSymbols);
			_graphStateManager.DisplayGraphChanged += OnDisplayGraphChanged;
			ExtensionDepth = 1;

			UpdateProjects();
		}

		private void OnDisplayGraphChanged(IBidirectionalGraph<DisplayNode, DisplayEdge> newGraph, GraphStatistics statistics)
		{
			Graph = newGraph;

			var reporter = GetStatsReporter(statistics);
			// TODO: this should be opt-in/hidden behind debug flag
			if (!Graph.IsVerticesEmpty)
			{
				_outputService.WriteLines(reporter.WriteStatistics(StatisticsReportContent.GraphingSpecific));
			}
		}

		private void OnDocumentInvalidated(DocumentId documentId) => _graphStateManager.InvalidateDocument(documentId);

		private void ResetNodeGraph()
		{
			Graph = Empty;
			_graphStateManager.InvalidateNodeGraph();
		}

		private void OnSolutionChanged()
		{
			ResetNodeGraph();
			UpdateProjects();
		}

		private void AddActiveDocumentAsRoot()
		{
			var active = _documentsService.GetActiveDocument();
			if (active == null)
			{
				// Ideally the command couldn't be executed if there were no active document
				return;
			}
			_shouldUseGitForRoots = false;
			if (_rootDocuments.Add(active))
			{
				_graphStateManager.InvalidateDisplayGraph();
			}
		}

		private void UseGitModifiedFilesAsRoot()
		{
			if (!_shouldUseGitForRoots)
			{
				_shouldUseGitForRoots = true;
				_graphStateManager.InvalidateDisplayGraph();
			}
		}

		private async Task<IList<ITypeSymbol>> GetCurrentRootSymbols(CancellationToken ct)
		{
			var rootDocuments = _shouldUseGitForRoots ?
					await (_gitService.GetAllModifiedAndNewFiles(ct))
					: _rootDocuments;

			await WriteLineAsync($"Building graph from {rootDocuments.Count} roots.", ct);

			var rootSymbols = await _roslynService.GetDeclaredSymbolsFromFilePaths(rootDocuments, ct).ToListAsync(ct);
			return rootSymbols;
		}


		private void ClearRoots()
		{
			_rootDocuments.Clear();
			_shouldUseGitForRoots = false;
			_graphStateManager.InvalidateDisplayGraph();
		}

		private void LogStats()
		{
			_joinableTaskFactory.RunAsync(async () =>
			{
				_outputService.WriteLine($"Gathering statistics for {Path.GetFileName(_solutionService.GetSolutionPath())}...");
				_outputService.FocusOutput();

				var ct = _statsRetrievalRegistration.GetNewToken();
				var stats = await _graphStateManager.GetStatisticsForFullGraph(ct);
				if (ct.IsCancellationRequested || stats == null)
				{
					return;
				}
				var reporter = GetStatsReporter(stats);

				_outputService.WriteLines(reporter.WriteStatistics(StatisticsReportContent.All));
				_outputService.FocusOutput();
			});

		}

		private static StatisticsReporter GetStatsReporter(GraphStatistics stats)
			=> new StatisticsReporter(stats, new ConsoleHeaderFormatter(), new MarkdownStyleTableFormatter(), CompactListFormatter.OpenCommaSeparated, CompactListFormatter.CurlyCommaSeparated, showTopXDeps: (20, 30), showTopXClusters: (10, 20));

		private void UpdateProjects()
		{
			var ct = _projectUpdatesRegistration.GetNewToken();
			_joinableTaskFactory.RunAsync(async () =>
			{
				var projects = await Task.Run(() => _roslynService.GetSortedProjects(), ct);
				if (ct.IsCancellationRequested)
				{
					return;
				}
				Projects = new SelectionList<ProjectIdentifier>(projects);
			});
		}

		private void OnActiveDocumentChanged()
		{
			var activeDocument = _documentsService.GetActiveDocument();
			SelectedNode = Graph.Vertices.FirstOrDefault(dn => dn.FilePath == activeDocument);
		}

		/// <summary>
		/// Write to output window from a background thread.
		/// </summary>
		private async Task WriteLineAsync(string line, CancellationToken ct)
		{
			await _joinableTaskFactory.SwitchToMainThreadAsync(ct);
			_outputService.WriteLine(line);
		}

		public void Dispose()
		{
			_graphStateManager.Dispose();
			_projectUpdatesRegistration.Dispose();
			_statsRetrievalRegistration.Dispose();

			_documentsService.ActiveDocumentChanged -= OnActiveDocumentChanged;
			_solutionService.SolutionChanged -= OnSolutionChanged;
			_modificationsService.DocumentInvalidated -= OnDocumentInvalidated;
			_modificationsService.SolutionInvalidated -= OnSolutionChanged;
			if (Projects != null)
			{
				Projects.SelectionChanged -= OnProjectsSelectionChanged;
			}
		}
	}
}
