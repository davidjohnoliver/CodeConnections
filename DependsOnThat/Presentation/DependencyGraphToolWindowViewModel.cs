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
using DependsOnThat.Input;
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
		private readonly ISettingsService _settingsService;
		private readonly JoinableTaskFactory _joinableTaskFactory;

		private readonly GraphStateManager _graphStateManager;

		private readonly SerialCancellationDisposable _projectUpdatesRegistration = new SerialCancellationDisposable();
		private readonly SerialCancellationDisposable _statsRetrievalRegistration = new SerialCancellationDisposable();

		public IBidirectionalGraph<DisplayNode, DisplayEdge> Graph { get => _graph; set => OnValueSet(ref _graph, value); }

		private IBidirectionalGraph<DisplayNode, DisplayEdge> _graph = Empty;
		private static IBidirectionalGraph<DisplayNode, DisplayEdge> Empty { get; } = new BidirectionalGraph<DisplayNode, DisplayEdge>();

		public bool IncludePureGenerated
		{
			get => _graphStateManager.IncludePureGenerated;
			set => OnValueSet(_graphStateManager.IncludePureGenerated, v => _graphStateManager.IncludePureGenerated = v, value);
		}

		public bool IncludeNestedTypes
		{
			get => _graphStateManager.IncludeNestedTypes;
			set => OnValueSet(_graphStateManager.IncludeNestedTypes, v => _graphStateManager.IncludeNestedTypes = v, value);
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

		public bool IsGitModeEnabled
		{
			get => _graphStateManager.IsGitModeEnabled;
			set => OnValueSet(_graphStateManager.IsGitModeEnabled, v => _graphStateManager.IsGitModeEnabled = v, value);
		}

		/// <summary>
		/// Should the active document (and its connections) automatically be included in the graph?
		/// </summary>
		public bool IsActiveAlwaysIncluded
		{
			get => _graphStateManager.IsActiveAlwaysIncluded;
			set => OnValueSet(_graphStateManager.IsActiveAlwaysIncluded, v => _graphStateManager.IsActiveAlwaysIncluded = v, value);
		}

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
					_graphStateManager.SetIncludedProjects(Projects?.SelectedItems);
				}
			}
		}

		private string[]? _excludedProjects;

		private void OnProjectsSelectionChanged()
		{
			_excludedProjects = GetExcludedProjects();
			_graphStateManager.SetIncludedProjects(Projects?.SelectedItems);
		}

		private GraphLayoutMode _graphLayoutMode = GraphLayoutMode.Hierarchy; // TODO: saved setting
		public GraphLayoutMode LayoutMode { get => _graphLayoutMode; set => OnValueSet(ref _graphLayoutMode, value); }
		public GraphLayoutMode[] LayoutModes { get; } = EnumUtils.GetValues<GraphLayoutMode>();

		public ICommand ClearRootsCommand { get; }
		public ICommand LogStatsCommand { get; }
		public IToggleCommand TogglePinnedCommand { get; }

		public DependencyGraphToolWindowViewModel(JoinableTaskFactory joinableTaskFactory, IDocumentsService documentsService, IRoslynService roslynService, IGitService gitService, ISolutionService solutionService, IOutputService outputService, IModificationsService modificationsService, ISettingsService settingsService)
		{
			_joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
			_documentsService = documentsService ?? throw new ArgumentNullException(nameof(documentsService));
			_roslynService = roslynService ?? throw new ArgumentNullException(nameof(roslynService));
			_gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
			_solutionService = solutionService ?? throw new ArgumentNullException(nameof(solutionService));
			_outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));
			_modificationsService = modificationsService ?? throw new ArgumentNullException(nameof(modificationsService));
			_settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
			_documentsService.ActiveDocumentChanged += OnActiveDocumentChanged;
			_solutionService.SolutionOpened += OnSolutionOpened;
			_solutionService.SolutionClosed += OnSolutionClosed;
			_settingsService.SolutionSettingsSaving += OnSolutionSettingsSaving;
			_modificationsService.DocumentInvalidated += OnDocumentInvalidated;
			_modificationsService.SolutionInvalidated += OnSolutionChanged;

			ClearRootsCommand = SimpleCommand.Create(ClearRoots);
			LogStatsCommand = SimpleCommand.Create(LogStats);
			TogglePinnedCommand = SimpleToggleCommand.Create<DisplayNode>(TogglePinned);

			_graphStateManager = new GraphStateManager(joinableTaskFactory, () => _roslynService.GetCurrentSolution(), getGitInfo: _gitService.GetAllModifiedAndNewFiles, getActiveDocument: _documentsService.GetActiveDocument, this);
			_graphStateManager.DisplayGraphChanged += OnDisplayGraphChanged;

			ApplySettings();
		}

		private void OnDisplayGraphChanged(IBidirectionalGraph<DisplayNode, DisplayEdge> newGraph, GraphStatistics statistics)
		{
			Graph = newGraph;

			SetActiveDocumentAsSelected();

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

		private void OnSolutionOpened()
		{
			ResetNodeGraph();
			ApplySettings();
		}

		private void OnSolutionClosed()
		{
			_projectUpdatesRegistration.Cancel();
			Projects = new SelectionList<ProjectIdentifier>();
			ResetNodeGraph();
		}

		private void ApplySettings()
		{
			var settings = _settingsService.LoadSolutionSettings();
			if (settings != null)
			{
				IncludePureGenerated = settings.IncludeGeneratedTypes;
				IsGitModeEnabled = settings.IsGitModeEnabled;
				IsActiveAlwaysIncluded = settings.IsActiveAlwaysIncluded;
				IncludeNestedTypes = settings.IncludeNestedTypes;
			}
			_excludedProjects = settings?.ExcludedProjects;
			UpdateProjects();
		}

		private string[]? GetExcludedProjects() => Projects?.UnselectedItems().Select(pi => pi.ProjectName).ToArray();

		private void OnSolutionSettingsSaving()
		{
			_settingsService.SaveSolutionSettings(new PersistedSolutionSettings(IncludePureGenerated, IsGitModeEnabled, _excludedProjects, IsActiveAlwaysIncluded, IncludeNestedTypes));
		}

		private void OnSolutionChanged()
		{
			ResetNodeGraph();
			UpdateProjects();
		}


		private void ClearRoots() => _graphStateManager.ClearSubgraph();

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

		private void TogglePinned(bool? toggleState, DisplayNode? displayNode)
		{
			if (displayNode != null)
			{
				_graphStateManager.TogglePinnedInSubgraph(displayNode.Key, toggleState ?? false);
			}
		}

		private static StatisticsReporter GetStatsReporter(GraphStatistics stats)
			=> new StatisticsReporter(stats, new ConsoleHeaderFormatter(), new MarkdownStyleTableFormatter(), CompactListFormatter.OpenCommaSeparated, CompactListFormatter.CurlyCommaSeparated, showTopXDeps: (20, 30), showTopXClusters: (10, 20));

		private void UpdateProjects()
		{
			var ct = _projectUpdatesRegistration.GetNewToken();
			var solutionPath = _solutionService.GetSolutionPath();
			_joinableTaskFactory.RunAsync(async () =>
			{
				var projects = await Task.Run(() => _roslynService.GetSortedProjects(), ct);
				if (ct.IsCancellationRequested)
				{
					return;
				}
				solutionPath?.ToString();
				Projects = new SelectionList<ProjectIdentifier>(projects);
				if (_excludedProjects != null)
				{
					foreach (var projectName in _excludedProjects)
					{
						if (Projects.FirstOrDefault(pi => pi.ProjectName == projectName) is { } project)
						{
							Projects.DeselectItem(project);
						}
					}
				}
			});
		}

		private void OnActiveDocumentChanged()
		{
			SetActiveDocumentAsSelected();
			if (IsActiveAlwaysIncluded)
			{
				_graphStateManager.InvalidateActiveDocument();
			}
		}

		private void SetActiveDocumentAsSelected()
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
			_solutionService.SolutionOpened -= OnSolutionOpened;
			_solutionService.SolutionClosed -= OnSolutionClosed;
			_settingsService.SolutionSettingsSaving -= OnSolutionSettingsSaving;
			_modificationsService.DocumentInvalidated -= OnDocumentInvalidated;
			_modificationsService.SolutionInvalidated -= OnSolutionChanged;
			if (Projects != null)
			{
				Projects.SelectionChanged -= OnProjectsSelectionChanged;
			}
		}
	}
}
