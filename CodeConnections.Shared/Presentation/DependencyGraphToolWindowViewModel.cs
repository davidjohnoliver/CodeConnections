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
using CodeConnections.Collections;
using CodeConnections.Disposables;
using CodeConnections.Extensions;
using CodeConnections.Graph;
using CodeConnections.Graph.Display;
using CodeConnections.Input;
using CodeConnections.Roslyn;
using CodeConnections.Services;
using CodeConnections.Statistics;
using CodeConnections.Text;
using CodeConnections.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Threading;
using QuickGraph;
using _IDisplayGraph = QuickGraph.IBidirectionalGraph<CodeConnections.Graph.Display.DisplayNode, CodeConnections.Graph.Display.DisplayEdge>;

namespace CodeConnections.Presentation
{
	internal sealed class DependencyGraphToolWindowViewModel : ViewModelBase, IDisposable
	{
		private readonly IDocumentsService _documentsService;
		private readonly IRoslynService _roslynService;
		private readonly IGitService _gitService;
		private readonly ISolutionService _solutionService;
		private readonly IOutputService _outputService;
		private readonly IModificationsService _modificationsService;
		private readonly ISolutionSettingsService _solutionSettingsService;
		private readonly IUserSettingsService _userSettingsService;
		private readonly JoinableTaskFactory _joinableTaskFactory;

		private readonly GraphUpdateManager _graphUpdateManager;

		private readonly SerialCancellationDisposable _projectUpdatesRegistration = new SerialCancellationDisposable();
		private readonly SerialCancellationDisposable _statsRetrievalRegistration = new SerialCancellationDisposable();

		public _IDisplayGraph Graph { get => _graph; set => OnValueSet(ref _graph, value); }

		private _IDisplayGraph _graph = Empty;
		private static _IDisplayGraph Empty { get; } = new BidirectionalGraph<DisplayNode, DisplayEdge>();

		public bool IncludePureGenerated
		{
			get => _graphUpdateManager.IncludePureGenerated;
			set => OnValueSet(_graphUpdateManager.IncludePureGenerated, v => _graphUpdateManager.IncludePureGenerated = v, value);
		}

		public bool IncludeNestedTypes
		{
			get => _graphUpdateManager.IncludeNestedTypes;
			set => OnValueSet(_graphUpdateManager.IncludeNestedTypes, v => _graphUpdateManager.IncludeNestedTypes = v, value);
		}

		public DisplayNode? SelectedNode
		{
			get => _graphUpdateManager.SelectedNode;
			set
			{
				if (OnValueSet(_graphUpdateManager.SelectedNode, v => _graphUpdateManager.SelectedNode = v, value))
				{
					if (value?.FilePath != null
						// Don't try to open the currently-open file. (This can, eg, disrupt the 'diff' view being opened from the source control changes window.)
						&& !PathUtils.AreEquivalent(value.FilePath, _documentsService.GetActiveDocument()))
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
					else
					{
						// If we didn't try to open the file, make sure selection is invalidated anyway (perhaps we're switching between two types in the same file)
						TryInvalidateSelection();
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
			get => _graphUpdateManager.IsGitModeEnabled;
			set => OnValueSet(_graphUpdateManager.IsGitModeEnabled, v => _graphUpdateManager.IsGitModeEnabled = v, value);
		}

		private bool _isImportantTypesModeEnabled;
		/// <summary>
		/// Should important types (as determined by the active mode) be automatically be included in the graph?
		/// </summary>
		public bool IsImportantTypesModeEnabled
		{
			get => _isImportantTypesModeEnabled;
			set
			{
				if (OnValueSet(ref _isImportantTypesModeEnabled, value))
				{
					//_userSettingsService.ApplySettings(_userSettingsService.GetSettings() with { IsImportantTypesModeEnabled = value }); // TODO: should be project settings, no? // But the *mode* should indeed be user settings!
					UpdateImportantTypesMode(true);
				}
			}
		}

		/// <summary>
		/// The 'real' value of ImportantTypesMode. This is always equal to the last chosen of either active-only or active-plus-connections, 
		/// whereas SelectedImportantTypesMode may be nulled out to facilitate the behaviour that making a selection in the combo box reactivates 
		/// IsImportantTypesModeEnabled.
		/// </summary>
		private ImportantTypesMode _implicitImportantTypesMode;

		private ImportantTypesMode? _selectedImportantTypesMode;
		public ImportantTypesMode? SelectedImportantTypesMode
		{
			get => _selectedImportantTypesMode;
			set
			{
				if (OnValueSet(ref _selectedImportantTypesMode, value))
				{
					if (value is { } selection)
					{
						_implicitImportantTypesMode = selection;
						//_userSettingsService.ApplySettings(_userSettingsService.GetSettings() with { ImportantTypesMode = selection }); // TODO now
						IsImportantTypesModeEnabled = true;
					}
					UpdateImportantTypesMode(false);
				}
			}
		}

		public ImportantTypesMode[] ImportantTypesModes { get; } = EnumUtils.GetValues<ImportantTypesMode>().Skip(1).ToArray();

		private bool _isActiveAlwaysIncluded;
		/// <summary>
		/// Should the active document (and its connections) automatically be included in the graph?
		/// </summary>
		public bool IsActiveAlwaysIncluded
		{
			get => _isActiveAlwaysIncluded;
			set
			{
				if (OnValueSet(ref _isActiveAlwaysIncluded, value))
				{
					_userSettingsService.ApplySettings(_userSettingsService.GetSettings() with { IsActiveAlwaysIncluded = value });
					UpdateIncludeActiveMode(true);
				}
			}
		}

		/// <summary>
		/// The 'real' value of IncludeActiveMode. This is always equal to the last chosen of either active-only or active-plus-connections, 
		/// whereas SelectedIncludeActiveMode may be nulled out to facilitate the behaviour that making a selection in the combo box reactivates 
		/// IsActiveAlwaysIncluded.
		/// </summary>
		private IncludeActiveMode _implicitIncludeActiveMode;

		private IncludeActiveMode? _selectedIncludeActiveMode;
		public IncludeActiveMode? SelectedIncludeActiveMode
		{
			get => _selectedIncludeActiveMode;
			set
			{
				if (OnValueSet(ref _selectedIncludeActiveMode, value))
				{
					if (value is { } selection)
					{
						_implicitIncludeActiveMode = selection;
						_userSettingsService.ApplySettings(_userSettingsService.GetSettings() with { IncludeActiveMode = selection });
						IsActiveAlwaysIncluded = true;
					}
					UpdateIncludeActiveMode(false);
				}
			}
		}

		public IncludeActiveMode[] IncludeActiveModes { get; } = EnumUtils.GetValues<IncludeActiveMode>().Skip(1).ToArray();

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
					_graphUpdateManager.SetIncludedProjects(Projects?.SelectedItems);
				}
			}
		}

		private string[]? _excludedProjects;

		private void OnProjectsSelectionChanged()
		{
			_excludedProjects = GetExcludedProjects();
			_graphUpdateManager.SetIncludedProjects(Projects?.SelectedItems);
		}

		private GraphLayoutMode _graphLayoutMode;
		public GraphLayoutMode LayoutMode
		{
			get => _graphLayoutMode; set
			{
				if (OnValueSet(ref _graphLayoutMode, value))
				{
					_userSettingsService.ApplySettings(_userSettingsService.GetSettings() with { LayoutMode = value });
				}
			}
		}
		public GraphLayoutMode[] LayoutModes { get; } = EnumUtils.GetValues<GraphLayoutMode>();

		public ICommand ClearRootsCommand { get; }
		public ICommand ShowAllNodesCommand { get; }
		public ICommand LogStatsCommand { get; }
		public ICommand DeselectAllProjectsCommand { get; }
		public ICommand SelectAllProjectsCommand { get; }

		public ICommand TogglePinnedMenuCommand { get; }

		public IToggleCommand TogglePinnedCommand { get; }

		public NodeCommands NodeCommands { get; }

		private int _maxAutomaticallyLoadedNodes;
		/// <summary>
		/// The maximum number of nodes to show without prompting explicit user opt-in.
		/// </summary>
		private int MaxAutomaticallyLoadedNodes
		{
			get => _maxAutomaticallyLoadedNodes;
			set
			{
				if (value != _maxAutomaticallyLoadedNodes)
				{
					_maxAutomaticallyLoadedNodes = value;

					if (ShouldShowUnloadedNodesWarning && _escrowedGraph != null && UnloadedNodesCount <= _maxAutomaticallyLoadedNodes)
					{
						ShowGraph(_escrowedGraph);
					}
				}
			}
		}

		/// <summary>
		/// If true, don't prompt user opt-in when loading large numbers of nodes.
		/// </summary>
		private bool _shouldLoadAnyNumberOfNodes = false;

		private bool _shouldShowUnloadedNodesWarning;
		/// <summary>
		/// Should warning message that display graph contains a large number of elements be visible?
		/// </summary>
		public bool ShouldShowUnloadedNodesWarning
		{
			get => _shouldShowUnloadedNodesWarning;
			set => OnValueSet(ref _shouldShowUnloadedNodesWarning, value);
		}

		private int _unloadedNodesCount;
		public int UnloadedNodesCount { get => _unloadedNodesCount; set => OnValueSet(ref _unloadedNodesCount, value); }

		/// <summary>
		/// If a graph isn't being displayed because it has a large number of nodes, it's stashed here.
		/// </summary>
		private _IDisplayGraph? _escrowedGraph;

		/// <summary>
		/// Has an exception already been logged?
		/// </summary>
		/// <remarks>Reset when the graph is updated successfully, the graph is clear, the solution changes, etc</remarks>
		private bool _hasLoggedException;

		private int _randomSeed = DateTime.Now.Millisecond;
		/// <summary>
		/// The current random seed.
		/// </summary>
		public int RandomSeed { get => _randomSeed; set => OnValueSet(ref _randomSeed, value); }

		private bool _isNodeGraphUpdating;
		/// <summary>
		/// Is <see cref="GraphUpdateManager"/> currently running an update?
		/// </summary>
		private bool IsNodeGraphUpdating
		{
			get => _isNodeGraphUpdating;
			set => OnValueSet(ref _isNodeGraphUpdating, value);
		}

		private bool _isGraphLayoutUpdating;
		/// <summary>
		/// Is the displayed graph currently updating its layout, etc?
		/// </summary>
		public bool IsGraphLayoutUpdating { get => _isGraphLayoutUpdating; set => OnValueSet(ref _isGraphLayoutUpdating, value); }

		/// <summary>
		/// Should the busy indicator be displayed?
		/// </summary>
		public bool IsBusy => Compose(IsNodeGraphUpdating, nameof(IsNodeGraphUpdating), IsGraphLayoutUpdating, nameof(IsGraphLayoutUpdating), (n, l) => n || l);

		private bool _enableDebugFeatures;

		/// <summary>
		/// Should additional features for debugging the tool be displayed?
		/// </summary>
		public bool EnableDebugFeatures
		{
			get => _enableDebugFeatures;
			set => OnValueSet(ref _enableDebugFeatures, value);
		}


#if DEBUG
		public DependencyGraphToolWindowViewModel() => throw new NotSupportedException("XAML Design usage");
#endif

		public DependencyGraphToolWindowViewModel(JoinableTaskFactory joinableTaskFactory, IDocumentsService documentsService, IRoslynService roslynService, IGitService gitService, ISolutionService solutionService, IOutputService outputService, IModificationsService modificationsService, ISolutionSettingsService solutionSettingsService, IUserSettingsService userSettingsService)
		{
			_joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
			_documentsService = documentsService ?? throw new ArgumentNullException(nameof(documentsService));
			_roslynService = roslynService ?? throw new ArgumentNullException(nameof(roslynService));
			_gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
			_solutionService = solutionService ?? throw new ArgumentNullException(nameof(solutionService));
			_outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));
			_modificationsService = modificationsService ?? throw new ArgumentNullException(nameof(modificationsService));
			_solutionSettingsService = solutionSettingsService ?? throw new ArgumentNullException(nameof(solutionSettingsService));
			_userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));

			_documentsService.ActiveDocumentChanged += OnActiveDocumentChanged;
			_solutionService.SolutionOpened += OnSolutionOpened;
			_solutionService.SolutionClosed += OnSolutionClosed;
			_solutionSettingsService.SolutionSettingsSaving += OnSolutionSettingsSaving;
			_userSettingsService.SettingsChanged += ApplyUserSettings;
			_modificationsService.DocumentInvalidated += OnDocumentInvalidated;
			_modificationsService.SolutionInvalidated += OnSolutionChanged;

			ClearRootsCommand = SimpleCommand.Create(ClearRoots);
			ShowAllNodesCommand = SimpleCommand.Create(ShowAllNodes);
			LogStatsCommand = SimpleCommand.Create(LogStats);
			DeselectAllProjectsCommand = SimpleCommand.Create(() => Projects?.DeselectAll());
			SelectAllProjectsCommand = SimpleCommand.Create(() => Projects?.SelectAll());

			TogglePinnedMenuCommand = SimpleCommand.Create<DisplayNode>(TogglePinned);

			TogglePinnedCommand = SimpleToggleCommand.Create<DisplayNode>(TogglePinned);

			_graphUpdateManager = new GraphUpdateManager(joinableTaskFactory, () => _roslynService.GetCurrentSolution(), _gitService.GetAllModifiedAndNewFiles, _documentsService.GetActiveDocument, outputService, this, () => _solutionService.IsSolutionOpening);
			_graphUpdateManager.DisplayGraphUpdating += OnDisplayGraphUpdating;
			_graphUpdateManager.DisplayGraphChanged += OnDisplayGraphChanged;
			_graphUpdateManager.UpdateFailed += OnGraphUpdateFailed;
			_graphUpdateManager.UpdateCompletedUnchanged += OnGraphUpdateCompletedUnchanged;

			NodeCommands = new(_graphUpdateManager);
			NodeCommands.AddOperationCommand(Subgraph.PinNodeAndNeighboursOp, "PinNodeAndNeighbours");
			NodeCommands.AddOperationCommand(Subgraph.AddInheritanceDependencyHierarchyOp, "AddInheritanceDependencyHierarchy");
			NodeCommands.AddOperationCommand(Subgraph.AddInheritanceDependentHierarchyOp, "AddInheritanceDependentHierarchy");
			NodeCommands.AddOperationCommand(Subgraph.AddDirectInheritanceDependentsOp, "AddInheritanceDirectDependents");
			NodeCommands.AddOperationCommand(Subgraph.AddAllInSameProjectOp, "AddAllInSameProject");
			NodeCommands.AddOperationCommand(Subgraph.AddAllInSolutionOp, "AddAllInSolution");

			ApplySolutionSettings();
			ApplyUserSettings();
		}

		private void OnDisplayGraphUpdating()
		{
			IsNodeGraphUpdating = true;
		}

		private void OnDisplayGraphChanged(_IDisplayGraph newGraph, GraphStatistics statistics)
		{
			_hasLoggedException = false;
			IsNodeGraphUpdating = false;

			var shouldShowGraph = newGraph.VertexCount <= MaxAutomaticallyLoadedNodes || _shouldLoadAnyNumberOfNodes;
			if (shouldShowGraph)
			{
				ShowGraph(newGraph);
			}
			else
			{
				ShouldShowUnloadedNodesWarning = true;
				UnloadedNodesCount = newGraph.VertexCount;
				_escrowedGraph = newGraph;
			}

			if (!Graph.IsVerticesEmpty && _outputService.IsEnabled(OutputLevel.Diagnostic))
			{
				var reporter = GetStatsReporter(statistics);
				_outputService.WriteLines(reporter.WriteStatistics(StatisticsReportContent.GraphingSpecific));
			}
		}

		private void ShowGraph(_IDisplayGraph? newGraph)
		{
			ShouldShowUnloadedNodesWarning = false;
			UnloadedNodesCount = 0;
			_escrowedGraph = null;

			Graph = newGraph ?? Empty;

			SetActiveDocumentAsSelected();
		}

		private void OnGraphUpdateFailed(Exception e)
		{
			_hasLoggedException = true;
			IsNodeGraphUpdating = false;

			var shouldShowException = (_outputService.IsEnabled(OutputLevel.Normal) && !_hasLoggedException)
				|| _outputService.IsEnabled(OutputLevel.Diagnostic);
			if (shouldShowException)
			{
				_outputService.WriteLine($"Graph update failed. {e}");
			}
		}

		private void OnGraphUpdateCompletedUnchanged() => IsNodeGraphUpdating = false;

		private void OnDocumentInvalidated(DocumentId documentId) => _graphUpdateManager.InvalidateDocument(documentId);

		private void ResetNodeGraph()
		{
			_shouldLoadAnyNumberOfNodes = false;
			_hasLoggedException = false;
			Graph = Empty;
			_graphUpdateManager.InvalidateNodeGraph(shouldWaitUntilIdle: true);
		}

		private void OnSolutionOpened()
		{
			ResetNodeGraph();
			ApplySolutionSettings();
		}

		private void OnSolutionClosed()
		{
			_projectUpdatesRegistration.Cancel();
			Projects = new SelectionList<ProjectIdentifier>();
			ResetNodeGraph();
		}

		private void ApplySolutionSettings()
		{
			var settings = _solutionSettingsService.LoadSolutionSettings();
			if (settings != null)
			{
				IncludePureGenerated = settings.IncludeGeneratedTypes;
				IsGitModeEnabled = settings.IsGitModeEnabled;
				IncludeNestedTypes = settings.IncludeNestedTypes;
			}
			_excludedProjects = settings?.ExcludedProjects;
			UpdateProjects();
		}

		private void ApplyUserSettings()
		{
			var settings = _userSettingsService.GetSettings();

			MaxAutomaticallyLoadedNodes = settings.MaxAutomaticallyLoadedNodes;
			LayoutMode = settings.LayoutMode;
			SelectedIncludeActiveMode = settings.IncludeActiveMode; // Set before IsActiveAlwaysIncluded, so it will be nulled out (but _implicitIncludeActiveMode retained) if IsActiveAlwaysIncluded=false
			IsActiveAlwaysIncluded = settings.IsActiveAlwaysIncluded;
			_outputService.CurrentOutputLevel = settings.OutputLevel;
			EnableDebugFeatures = settings.EnableDebugFeatures;
		}

		private string[]? GetExcludedProjects() => Projects?.UnselectedItems().Select(pi => pi.ProjectName).ToArray();

		private void OnSolutionSettingsSaving()
		{
			_solutionSettingsService.SaveSolutionSettings(new PersistedSolutionSettings(IncludePureGenerated, IsGitModeEnabled, _excludedProjects, IncludeNestedTypes));
		}

		private void OnSolutionChanged()
		{
			ResetNodeGraph();
			UpdateProjects();
		}


		private void ClearRoots()
		{
			RerollRandomSeed();
			IsGitModeEnabled = false;
			IsImportantTypesModeEnabled = false;
			_shouldLoadAnyNumberOfNodes = false;
			_hasLoggedException = false;
			Graph = Empty;
			_graphUpdateManager.ClearSubgraph();
		}

		/// <summary>
		/// Change the current random seed to a new value.
		/// </summary>
		private void RerollRandomSeed()
		{
			var newSeed = DateTime.Now.Millisecond;
			if (newSeed == RandomSeed)
			{
				newSeed++; // Hey there's a 1/1000 chance
			}
			RandomSeed = newSeed;
		}

		private void ShowAllNodes()
		{
			_shouldLoadAnyNumberOfNodes = true;
			ShowGraph(_escrowedGraph);
		}

		private void LogStats()
		{
			_joinableTaskFactory.RunAsync(async () =>
			{
				_outputService.WriteLine($"Gathering statistics for {Path.GetFileName(_solutionService.GetSolutionPath())}...");
				_outputService.FocusOutput();

				var ct = _statsRetrievalRegistration.GetNewToken();
				var stats = await _graphUpdateManager.GetStatisticsForFullGraph(ct);
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
				_graphUpdateManager.TogglePinnedInSubgraph(displayNode.Key, toggleState ?? false);
			}
		}

		private void TogglePinned(DisplayNode? displayNode)
		{
			if (displayNode != null)
			{
				var newIsPinned = !displayNode.IsPinned;
				_graphUpdateManager.TogglePinnedInSubgraph(displayNode.Key, newIsPinned);
				displayNode.IsPinned = newIsPinned;
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
			TryInvalidateSelection();
		}

		private void TryInvalidateSelection()
		{
			if (IsActiveAlwaysIncluded)
			{
				_graphUpdateManager.InvalidateActiveDocumentOrSelection();
			}
		}

		private void SetActiveDocumentAsSelected()
		{
			var activeDocument = _documentsService.GetActiveDocument();
			bool DoesPathMatchActive(DisplayNode? dn) => PathUtils.AreEquivalent(dn?.FilePath, activeDocument);

			// Keep current selection if it matches path of active document, otherwise select first vertex found which does match
			if (!DoesPathMatchActive(SelectedNode))
			{
				SelectedNode = Graph.Vertices.FirstOrDefault(DoesPathMatchActive);
			}
		}

		/// <summary>
		/// Apply the effective <see cref="IncludeActiveMode"/> to the update manager.
		/// </summary>
		/// <param name="isSwitching">Is <see cref="IsActiveAlwaysIncluded"/> changing?</param>
		private void UpdateIncludeActiveMode(bool isSwitching)
		{
			if (isSwitching)
			{
				if (IsActiveAlwaysIncluded)
				{
					SelectedIncludeActiveMode = _implicitIncludeActiveMode;
				}
			}

			if (!IsActiveAlwaysIncluded)
			{
				SelectedIncludeActiveMode = null;
			}

			_graphUpdateManager.IncludeActiveMode = SelectedIncludeActiveMode ?? IncludeActiveMode.DontInclude;
		}

		/// <summary>
		/// Apply the effective <see cref="ImportantTypesMode"/> to the update manager.
		/// </summary>
		/// <param name="isSwitching">Is <see cref="IsImportantTypesModeEnabled"/> changing?</param>
		private void UpdateImportantTypesMode(bool isSwitching)
		{
			if (isSwitching)
			{
				if (IsImportantTypesModeEnabled)
				{
					SelectedImportantTypesMode = _implicitImportantTypesMode;
				}
			}

			if (!IsImportantTypesModeEnabled)
			{
				SelectedImportantTypesMode = null;
			}

			_graphUpdateManager.ImportantTypesMode = SelectedImportantTypesMode ?? ImportantTypesMode.None;
		}

		public void Dispose()
		{
			_graphUpdateManager.Dispose();
			_projectUpdatesRegistration.Dispose();
			_statsRetrievalRegistration.Dispose();

			_documentsService.ActiveDocumentChanged -= OnActiveDocumentChanged;
			_solutionService.SolutionOpened -= OnSolutionOpened;
			_solutionService.SolutionClosed -= OnSolutionClosed;
			_solutionSettingsService.SolutionSettingsSaving -= OnSolutionSettingsSaving;
			_modificationsService.DocumentInvalidated -= OnDocumentInvalidated;
			_modificationsService.SolutionInvalidated -= OnSolutionChanged;
			if (Projects != null)
			{
				Projects.SelectionChanged -= OnProjectsSelectionChanged;
			}
		}
	}
}
