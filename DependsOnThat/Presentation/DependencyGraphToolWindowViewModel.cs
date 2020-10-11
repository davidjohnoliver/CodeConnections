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
using DependsOnThat.Roslyn;
using DependsOnThat.Services;
using DependsOnThat.Statistics;
using DependsOnThat.Text;
using DependsOnThat.Utilities;
using Microsoft.VisualStudio.Threading;
using QuickGraph;

namespace DependsOnThat.Presentation
{
	internal class DependencyGraphToolWindowViewModel : ViewModelBase, IDisposable
	{
		private readonly IDocumentsService _documentsService;
		private readonly IRoslynService _roslynService;
		private readonly IGitService _gitService;
		private readonly ISolutionService _solutionService;
		private readonly IOutputService _outputService;
		private readonly JoinableTaskFactory _joinableTaskFactory;
		private readonly HashSet<string> _rootDocuments = new HashSet<string>();
		private readonly SerialCancellationDisposable _graphUpdatesRegistration = new SerialCancellationDisposable();
		private readonly SerialCancellationDisposable _projectUpdatesRegistration = new SerialCancellationDisposable();


		private IBidirectionalGraph<DisplayNode, DisplayEdge> _graph = Empty;
		private bool _shouldUseGitForRoots;

		public IBidirectionalGraph<DisplayNode, DisplayEdge> Graph { get => _graph; set => OnValueSet(ref _graph, value); }

		private static IBidirectionalGraph<DisplayNode, DisplayEdge> Empty { get; } = new BidirectionalGraph<DisplayNode, DisplayEdge>();

		private int _extensionDepth = 1;
		public int ExtensionDepth
		{
			get => _extensionDepth;
			set
			{
				if (OnValueSet(ref _extensionDepth, value))
				{
					TryUpdateGraph();
				}
			}
		}

		private bool _excludePureGenerated;
		public bool ExcludePureGenerated
		{
			get => _excludePureGenerated;
			set
			{
				if (OnValueSet(ref _excludePureGenerated, value))
				{
					TryUpdateGraph();
				}
			}
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

		private void OnProjectsSelectionChanged() => TryUpdateGraph();

		public ICommand AddActiveDocumentAsRootCommand { get; }
		public ICommand ClearRootsCommand { get; }
		public ICommand UseGitModifiedFilesAsRootCommand { get; }

		public ICommand LogStatsCommand { get; }

		public DependencyGraphToolWindowViewModel(JoinableTaskFactory joinableTaskFactory, IDocumentsService documentsService, IRoslynService roslynService, IGitService gitService, ISolutionService solutionService, IOutputService outputService)
		{
			_joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
			_documentsService = documentsService ?? throw new ArgumentNullException(nameof(documentsService));
			_documentsService.ActiveDocumentChanged += OnActiveDocumentChanged;
			_roslynService = roslynService ?? throw new ArgumentNullException(nameof(roslynService));
			_gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
			_solutionService = solutionService ?? throw new ArgumentNullException(nameof(solutionService));
			_outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));
			_solutionService.SolutionChanged += OnSolutionChanged;

			AddActiveDocumentAsRootCommand = SimpleCommand.Create(AddActiveDocumentAsRoot);
			ClearRootsCommand = SimpleCommand.Create(ClearRoots);
			UseGitModifiedFilesAsRootCommand = SimpleCommand.Create(UseGitModifiedFilesAsRoot);
			LogStatsCommand = SimpleCommand.Create(LogStats);

			UpdateProjects();
		}

		private void OnSolutionChanged()
		{
			ResetGraph();
			UpdateProjects();
		}

		private void ResetGraph()
		{
			_graphUpdatesRegistration.Cancel();
			Graph = Empty;
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
				TryUpdateGraph();
			}
		}

		private void UseGitModifiedFilesAsRoot()
		{
			_shouldUseGitForRoots = true;
			TryUpdateGraph();
		}

		private void TryUpdateGraph()
		{
			GraphingTime = null;
			GraphingError = null;
			_joinableTaskFactory.RunAsync(async () =>
			{
				var stopwatch = Stopwatch.StartNew();
				try
				{
					var (graph, statsReporter) = await GetGraphAsync(_graphUpdatesRegistration.GetNewToken());
					stopwatch.Stop();
					if (graph == null)
					{
						return;
					}
					if (graph.VertexCount > 0)
					{
						GraphingTime = stopwatch.Elapsed;
						_outputService.WriteLine($"Analyzing connections in background took about {TimeUtils.GetRoundedTime(GraphingTime.Value, CultureInfo.CurrentCulture)} for {graph.VertexCount} nodes.");
					}
					Graph = graph;
					_outputService.WriteLines(statsReporter!.WriteGraphingSpecificStatistics());
				}
				catch (Exception e)
				{
					stopwatch.Stop();
					GraphingError = $"Error: {e}";
				}
			});
		}

		private Task<(IBidirectionalGraph<DisplayNode, DisplayEdge>?, StatisticsReporter?)> GetGraphAsync(CancellationToken ct)
		{
			var includedProjects = Projects?.SelectedItems.ToArray();
			return Task.Run<(IBidirectionalGraph<DisplayNode, DisplayEdge>?, StatisticsReporter?)>(async () =>
			{
				if (ct.IsCancellationRequested)
				{
					return (null, null);
				}

				var rootDocuments = _shouldUseGitForRoots ?
					await (_gitService.GetAllModifiedAndNewFiles(ct))
					: _rootDocuments;

				await WriteLineAsync($"Building graph from {rootDocuments.Count} roots.", ct);

				var rootSymbols = await _roslynService.GetDeclaredSymbolsFromFilePaths(rootDocuments, ct).ToListAsync(ct);
				if (rootSymbols.Count == 0)
				{
					return (Empty, null);
				}
				var nodeGraph = await NodeGraph.BuildGraph(_roslynService.GetCurrentSolution(), includedProjects, ExcludePureGenerated, ct);
				var graph = nodeGraph.GetDisplaySubgraph(rootSymbols, ExtensionDepth);

				if (ct.IsCancellationRequested)
				{
					return (null, null);
				}

				var stats = GraphStatistics.GetForSubgraph(graph);

				return (graph, GetStatsReporter(stats));
			}, ct);
		}

		private void ClearRoots()
		{
			_rootDocuments.Clear();
			_shouldUseGitForRoots = false;
			TryUpdateGraph();
		}

		private void LogStats()
		{
			_joinableTaskFactory.RunAsync(async () =>
			{
				_outputService.WriteLine($"Gathering statistics for {Path.GetFileName(_solutionService.GetSolutionPath())}...");
				_outputService.FocusOutput();
				var includedProjects = Projects?.SelectedItems.ToArray();
				var statsWriter = await Task.Run(async () =>
				{
					var nodeGraph = await NodeGraph.BuildGraph(_roslynService.GetCurrentSolution(), includedProjects, ExcludePureGenerated, CancellationToken.None);
					var stats = GraphStatistics.GetForFullGraph(nodeGraph);
					return GetStatsReporter(stats);
				});

				_outputService.WriteLines(statsWriter.WriteGeneralStatistics());
				_outputService.WriteLines(statsWriter.WriteGraphingSpecificStatistics());
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
			_graphUpdatesRegistration.Dispose();
			_projectUpdatesRegistration.Dispose();
			_documentsService.ActiveDocumentChanged -= OnActiveDocumentChanged;
			_solutionService.SolutionChanged -= OnSolutionChanged;
			if (Projects != null)
			{
				Projects.SelectionChanged -= OnProjectsSelectionChanged;
			}
		}
	}
}
