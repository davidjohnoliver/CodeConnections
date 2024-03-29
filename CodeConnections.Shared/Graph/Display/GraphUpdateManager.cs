﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeConnections.Disposables;
using CodeConnections.Roslyn;
using Microsoft.CodeAnalysis;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;
using Microsoft.VisualStudio.Threading;
using QuickGraph;
using CodeConnections.Extensions;
using CodeConnections.Utilities;
using CodeConnections.Statistics;
using CodeConnections.Git;
using System.Diagnostics.CodeAnalysis;
using CodeConnections.Services;
using CodeConnections.Presentation;
using System.Diagnostics;

namespace CodeConnections.Graph.Display
{
	/// <summary>
	/// Manages invalidation and asynchronous updating of <see cref="NodeGraph"/> and display graph state.
	/// </summary>
	public sealed class GraphUpdateManager : IDisposable
	{
		private bool _includePureGenerated;
		/// <summary>
		/// Should types defined exclusively in generated code be included?
		/// </summary>
		public bool IncludePureGenerated
		{
			get => _includePureGenerated;
			set
			{
				_includePureGenerated = value;
				InvalidateNodeGraph(shouldWaitUntilIdle: false);
			}
		}

		private bool _includeNestedTypes = true;
		public bool IncludeNestedTypes
		{
			get => _includeNestedTypes;
			set
			{
				var hasChanged = value != _includeNestedTypes;
				_includeNestedTypes = value;
				if (hasChanged)
				{
					InvalidateSubgraph();
				}
			}
		}

		private bool _isGitModeEnabled;
		/// <summary>
		/// When in 'Git mode', Git info should be calculated and applied for nodes, and modified files should be inserted into the 
		/// displayed subgraph.
		/// </summary>
		public bool IsGitModeEnabled
		{
			get => _isGitModeEnabled;
			set
			{
				var previous = _isGitModeEnabled;
				_isGitModeEnabled = value;

				switch (previous, _isGitModeEnabled)
				{
					case (true, false):
						_previousGitInfos = ArrayUtils.GetEmpty<GitInfo>();
						break;
					case (false, true):
						EnsureStepReruns(UpdateState.UpdatingGitInfo);
						break;
				}
			}
		}

		private IncludeActiveMode _includeActiveMode;
		public IncludeActiveMode IncludeActiveMode
		{
			get => _includeActiveMode;
			set
			{
				var didChange = value != _includeActiveMode;
				if (didChange)
				{
					_includeActiveMode = value;
					ClearUnpinnedNextRun();
					InvalidateActiveDocumentOrSelection();
				}
			}
		}

		private ImportantTypesMode _importantTypesMode;
		public ImportantTypesMode ImportantTypesMode
		{
			get => _importantTypesMode;
			set
			{
				if (value != _importantTypesMode)
				{
					_currentClassifier = null;
					_importantTypesMode = value;
					EnsureStepReruns(UpdateState.UpdatingImportantTypes);
				}
			}
		}

		public int MaxAutomaticallyLoadedNodes { get; set; }

		private IntOrAuto _numberOfImportantTypesRequested = IntOrAuto.Auto; // Default before loading solution settings
		public IntOrAuto NumberOfImportantTypesRequested
		{
			get => _numberOfImportantTypesRequested;
			set
			{
				if (_numberOfImportantTypesRequested != value)
				{
					_numberOfImportantTypesRequested = value;
					EnsureStepReruns(UpdateState.UpdatingImportantTypes);
				}
			}
		}
		private DisplayNode? _selectedNode;
		public DisplayNode? SelectedNode
		{
			get => _selectedNode;
			set
			{
				if (!Equals(value, _selectedNode))
				{
					ClearUnpinnedNextRun();
				}
				_selectedNode = value;
			}
		}

		/// <summary>
		/// Raised whenever an active update begins.
		/// </summary>
		public event Action? DisplayGraphUpdating;

		/// <summary>
		/// Raised whenever a new copy of the display graph is created.
		/// </summary>
		public event Action<IBidirectionalGraph<DisplayNode, DisplayEdge>, GraphStatistics>? DisplayGraphChanged;

		/// <summary>
		/// Raised when an update failed to complete.
		/// </summary>
		public event Action<Exception>? UpdateFailed;

		/// <summary>
		/// Raised when an update completed without modifying the display graph.
		/// </summary>
		public event Action? UpdateCompletedUnchanged;

		private UpdateState _currentUpdateState = UpdateState.NotUpdating;
		private readonly SerialCancellationDisposable _updateSubscription = new SerialCancellationDisposable();
		private readonly IdleTimer _documentEditIdleTimer = new IdleTimer(idleWaitTimeMS: 2000);
		private readonly IdleTimer _graphRebuildIdleTimer = new IdleTimer(idleWaitTimeMS: 3000);

		private ProjectIdentifier[]? _includedProjects;
		private NodeGraph? _nodeGraph;

		private ImportantTypesClassifier? _currentClassifier;

		/// <summary>
		/// Documents that need to be updated.
		/// </summary>
		private readonly HashSet<DocumentId> _invalidatedDocuments = new HashSet<DocumentId>();

		private readonly JoinableTaskFactory _joinableTaskFactory;
		private readonly Func<Solution> _getCurrentSolution;
		private readonly Func<CancellationToken, Task<ICollection<GitInfo>>> _getGitInfo;
		private readonly Func<string?> _getActiveDocument;
		private readonly Func<bool> _isSolutionStillOpening;
		private readonly IOutputService _outputService;

		private readonly object _nodeParentContext;

		private bool _needsDisplayGraphUpdate;
		/// <summary>
		/// When this flag is set, indicates that another update should be run immediately after the current one.
		/// </summary>
		private bool _needsRerun;
		/// <summary>
		/// When this flag is set, indicates that unpinned nodes should be removed from the graph on the next run.
		/// </summary>
		private bool _shouldClearUnpinned;

		private ICollection<GitInfo> _previousGitInfos = ArrayUtils.GetEmpty<GitInfo>();

		/// <summary>
		/// Backing object for statistics task. This will only be non-null when the view model is awaiting graph statistics.
		/// </summary>
		private TaskCompletionSource<GraphStatistics?>? _statisticsTCS;

		private Subgraph _includedNodes;
		private readonly List<Subgraph.Operation> _pendingSubgraphOperations = new();

		/// <summary>
		/// The number of NodeGraph rebuilds currently in flight in the background.
		/// </summary>
		private int _backgroundGraphRebuildsActive;

		internal GraphUpdateManager(JoinableTaskFactory joinableTaskFactory, Func<Solution> getCurrentSolution, Func<CancellationToken, Task<ICollection<GitInfo>>> getGitInfo, Func<string?> getActiveDocument, IOutputService outputService, object nodeParentContext, Func<bool> isSolutionStillOpening)
		{
			_joinableTaskFactory = joinableTaskFactory;
			_getCurrentSolution = getCurrentSolution ?? throw new ArgumentNullException(nameof(getCurrentSolution));
			_getGitInfo = getGitInfo ?? throw new ArgumentNullException(nameof(getGitInfo));
			_getActiveDocument = getActiveDocument;
			_nodeParentContext = nodeParentContext;
			_isSolutionStillOpening = isSolutionStillOpening ?? throw new ArgumentNullException(nameof(isSolutionStillOpening));
			_outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));
			ClearSubgraphAndPendingOperations();
		}

		/// <summary>
		/// Invalidate the current <see cref="NodeGraph"/> completely, requiring it to be rebuilt.
		/// </summary>
		public void InvalidateNodeGraph(bool shouldWaitUntilIdle)
		{
			ThreadUtils.ThrowIfNotOnUIThread();

			_invalidatedDocuments.Clear();
			_nodeGraph = null;
			if (shouldWaitUntilIdle)
			{
				_graphRebuildIdleTimer.RecordActive();
			}
			ClearSubgraphAndPendingOperations();
			RunUpdate(waitForIdle: false);
		}

		/// <summary>
		/// Clear included nodes and any pending operations.
		/// </summary>
		/// <returns>True if there were previously included nodes, false otherwise. </returns>
		[MemberNotNull(nameof(_includedNodes))]
		private bool ClearSubgraphAndPendingOperations()
		{
			_pendingSubgraphOperations.Clear();
			var hadNodes = _includedNodes?.Count > 0;
			_includedNodes = new(ShouldIncludeNodeInSubgraph);
			_previousGitInfos = ArrayUtils.GetEmpty<GitInfo>(); // Ensure Git nodes are re-added if needed
			return hadNodes;
		}

		private bool ShouldIncludeNodeInSubgraph(NodeKey nodeKey, NodeGraph nodeGraph)
		{
			var node = nodeGraph.Nodes.GetOrDefaultFromReadOnly(nodeKey);
			if (!IncludeNestedTypes && ((node as TypeNode)?.IsNestedType ?? false))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Invalidate the document corresponding to <paramref name="documentId"/>, triggering a partial update of the <see cref="NodeGraph"/>.
		/// </summary>
		public void InvalidateDocument(DocumentId documentId)
		{
			ThreadUtils.ThrowIfNotOnUIThread();

			_invalidatedDocuments.Add(documentId);

			_documentEditIdleTimer.RecordActive();

			if (_currentUpdateState != UpdateState.NotUpdating)
			{
				// Nothing to do. If we're rebuilding NodeGraph, the invalidated document will be considered during incremental update phase;
				// else if _invalidatedDocuments is non-empty at the end of the update, another update will be queued up then.
			}

			RunUpdate(waitForIdle: true);
		}

		/// <summary>
		/// Invalidate the display graph, causing it to be rebuilt.
		/// </summary>
		private void InvalidateDisplayGraph()
		{
			ThreadUtils.ThrowIfNotOnUIThread();

			_needsDisplayGraphUpdate = true;

			if (_currentUpdateState is > UpdateState.WaitingForIdle and < UpdateState.RebuildingDisplayGraph)
			{
				// Nothing to do, the display graph update will take place with up-to-date parameters after node graph is updated
				return;
			}
			else
			{
				// If we're not updating, we launch an update. If we were updating display graph, we cancel it and start over.
				RunUpdate(waitForIdle: false);
			}
		}

		/// <summary>
		/// Fully rebuild the subgraph.
		/// </summary>
		private void InvalidateSubgraph()
		{
			ClearSubgraph();
			if (_currentUpdateState is > UpdateState.WaitingForIdle and < UpdateState.UpdatingSubgraph)
			{
				// Nothing to do, the subgraph update will take place with up-to-date parameters after node graph is updated
				return;
			}
			else
			{
				// If we're not updating, we launch an update. If we were updating subgraph, we cancel it and start over.
				RunUpdate(waitForIdle: false);
			}
		}

		/// <summary>
		/// Set the projects in the solution that should be included in the graph. If null, all projects will be included.
		/// </summary>
		public void SetIncludedProjects(IEnumerable<ProjectIdentifier>? includedProjects)
		{
			_includedProjects = includedProjects?.ToArray();
			InvalidateNodeGraph(shouldWaitUntilIdle: false);
		}

		public void ModifySubgraph(Subgraph.Operation operation)
		{
			ThreadUtils.ThrowIfNotOnUIThread();

			_pendingSubgraphOperations.Add(operation);

			if (_currentUpdateState > UpdateState.WaitingForIdle)
			{
				// Nothing to do. Either we haven't reached subgraph updates and it'll be considered then, or we've passed it and another update will 
				// be queued up at the end of the current one.
				return;
			}

			RunUpdate(waitForIdle: false);
		}

		public void ClearSubgraph()
		{
			ThreadUtils.ThrowIfNotOnUIThread();

			if (ClearSubgraphAndPendingOperations())
			{
				InvalidateDisplayGraph();
				InvalidateActiveDocumentOrSelection();
			}
		}

		public void InvalidateActiveDocumentOrSelection() => EnsureStepReruns(UpdateState.IncludeActive);

		public void ClearUnpinnedNextRun() => _shouldClearUnpinned = true;

		/// <summary>
		/// Returns a <see cref="GraphStatistics"/> object describing the full <see cref="NodeGraph"/>.
		/// </summary>
		public Task<GraphStatistics?> GetStatisticsForFullGraph(CancellationToken ct)
		{
			_statisticsTCS?.TrySetResult(null);
			TaskCompletionSource<GraphStatistics?> tcs = new TaskCompletionSource<GraphStatistics?>();
			_statisticsTCS = tcs;
			if (_currentUpdateState <= UpdateState.WaitingForIdle)
			{
				RunUpdate(waitForIdle: false);
			}
			ct.Register(() =>
			{
				tcs.TrySetResult(null);
				if (_statisticsTCS == tcs)
				{
					_statisticsTCS = null;
				}
			});
			return tcs.Task;
		}

		/// <summary>
		/// Synchronously set <paramref name="node"/> to pinned or unpinned in the subgraph of included nodes. This will not trigger an 
		/// asynchronous update, and won't do anything if the node isn't found in the subgraph.
		/// </summary>
		public void TogglePinnedInSubgraph(NodeKey node, bool setPinned) => _includedNodes.TogglePinned(node, setPinned);

		/// <summary>
		/// Ensure that the <paramref name="stepToRerun"/> update step runs. If an update is active and <paramref name="stepToRerun"/> is 
		/// running or has run, allow the update to complete and then run a new one.
		/// </summary>
		/// <param name="stepToRerun"></param>
		private void EnsureStepReruns(UpdateState stepToRerun)
		{
			if (_currentUpdateState <= UpdateState.WaitingForIdle)
			{
				// No update running, run one
				RunUpdate(waitForIdle: false);
				return;
			}

			if (_currentUpdateState >= stepToRerun)
			{
				// Step is already running or been passed - ensure a new update runs so the step can run with up-to-date values
				_needsRerun = true;
			}

			// Wait for step to be reached or rerun to launch
		}

		/// <summary>
		/// Synchronous entry point for the main update routine.
		/// </summary>
		/// <param name="waitForIdle">If true, the update should be delayed until after an idle state (no user input) is reached. If false, it should run immediately.</param>
		private void RunUpdate(bool waitForIdle)
		{
			_needsRerun = false;

			ThreadUtils.ThrowIfNotOnUIThread();

			if (_currentUpdateState == UpdateState.WaitingForIdle)
			{
				if (!waitForIdle)
				{
					_documentEditIdleTimer.FastTrackTimer();
					// The idling update will execute
					return;
				}
				else
				{
					// An update is already running and waiting for idle, nothing to do
					return;
				}
			}

			var ct = _updateSubscription.GetNewToken();
			var _ = _joinableTaskFactory.RunAsync(async () =>
			{

				var displayGraph = await Update(waitForIdle, ct);

				// Save the cancellation state, since we are committing to the graph change at this point; we still want to rerun if
				// required, even (especially) if a cancellation is subsequently triggered
				var wasCanceled = ct.IsCancellationRequested;

				if (!wasCanceled && displayGraph.Graph != null && displayGraph.Stats != null)
				{
					if (_outputService.IsEnabled(OutputLevel.Diagnostic))
					{
						_outputService.WriteLine($"Updated display graph. Vertex count: {displayGraph.Graph.VertexCount}");
					}
					DisplayGraphChanged?.Invoke(displayGraph.Graph, displayGraph.Stats);
				}

				if (wasCanceled)
				{
					return;
				}
				else if (NeedsRerunNow())
				{
					RunUpdate(waitForIdle: false);
				}
				else if (NeedsRerunOnIdle())
				{
					RunUpdate(waitForIdle: true);
				}

				bool NeedsRerunNow() =>
					// Rerun was explicitly requested
					_needsRerun ||
					// This can happen if stats log was requested while building display graph
					_statisticsTCS != null ||
					// Run another update if we have uncommitted subgraph operations
					_pendingSubgraphOperations.Count > 0;

				// Run another update if we need incremental NodeGraph update
				bool NeedsRerunOnIdle() => _invalidatedDocuments.Count > 0;
			});
		}

		/// <summary>
		/// The main update routine. This consists of different phases (as defined by <see cref="UpdateState"/>) which will be run as needed,
		/// as determined when the phase is reached.
		/// 
		/// There should only ever be at most one active (ie not cancelled) Update in flight at one time. We can enforce this because the
		/// entry point is only ever called on the main thread.
		/// </summary>
		/// <param name="waitForIdle">
		/// Whether the update should wait for an idle state before proceeding.
		/// 
		/// Even when true, this may be interrupted by subsequent invalidations that require an immediate update.
		/// </param>
		/// <returns>The updated display graph if it is rebuilt, or null otherwise.</returns>
		private async Task<(IBidirectionalGraph<DisplayNode, DisplayEdge>? Graph, GraphStatistics? Stats)> Update(bool waitForIdle, CancellationToken ct)
		{
			ThreadUtils.ThrowIfNotOnUIThread();

			CompilationCache? compilationCache = null;
			CompilationCache EnsureCompilationCache()
			{
				var cache = compilationCache ?? new CompilationCache();
				var solution = _getCurrentSolution();
				cache.SetSolution(solution);
				return cache;
			}

			try
			{
				if (waitForIdle)
				{
					_currentUpdateState = UpdateState.WaitingForIdle;
					await _documentEditIdleTimer.WaitForIdle(ct);
				}

				if (!ct.IsCancellationRequested)
				{
					DisplayGraphUpdating?.Invoke();
				}

				if (_nodeGraph == null)
				{
					_currentUpdateState = UpdateState.RebuildingNodeGraph;
					Stopwatch? stopwatch = null;
					if (_outputService.IsEnabled(OutputLevel.Diagnostic))
					{
						_outputService.WriteLine("Rebuilding node graph...");
						stopwatch = new();
						stopwatch.Start();
					}

					compilationCache = EnsureCompilationCache();
					var nodeGraph = await RebuildNodeGraph(compilationCache, ct);
					if (!ct.IsCancellationRequested)
					{

						if (_outputService.IsEnabled(OutputLevel.Diagnostic))
						{
							var outputText = "Node graph rebuild completed. ";
							if (stopwatch != null)
							{
								stopwatch.Stop();
								outputText += $"Took {stopwatch.ElapsedMilliseconds} ms to build {nodeGraph?.Nodes.Count ?? 0} nodes.";
							}
							_outputService.WriteLine(outputText);
						}
						_nodeGraph = nodeGraph;
					}
					_needsDisplayGraphUpdate = true;
				}

				if (ct.IsCancellationRequested || _nodeGraph == null)
				{
					return (null, null);
				}

				if (_invalidatedDocuments.Count > 0)
				{
					_currentUpdateState = UpdateState.UpdatingNodeGraph;
					var invalidatedDocuments = _invalidatedDocuments.ToList();
					_invalidatedDocuments.Clear();
					compilationCache = EnsureCompilationCache();
					var alteredNodes = await _nodeGraph.Update(compilationCache, invalidatedDocuments, ct);
					_needsDisplayGraphUpdate |= alteredNodes?.Count > 0;
				}

				if (ct.IsCancellationRequested)
				{
					return (null, null);
				}

				_currentUpdateState = UpdateState.UpdatingGitInfo;
				if (IsGitModeEnabled)
				{
					await AnnotateGitInfo(ct);
				}
				else
				{
					TryClearCategory(Subgraph.InclusionCategory.GitChanges, true);
				}

				if (ct.IsCancellationRequested)
				{
					return (null, null);
				}

				if (_nodeGraph != null)
				{
					_currentUpdateState = UpdateState.UpdatingImportantTypes;
					if (ImportantTypesMode == ImportantTypesMode.None)
					{
						TryClearCategories(Subgraph.ImportanceSet, false);
					}
					else
					{
						_currentClassifier ??= ImportantTypesClassifier.GetForMode(ImportantTypesMode, MaxAutomaticallyLoadedNodes);
						ModifySubgraph(Subgraph.UpdateImportantTypesOp(_currentClassifier.GetImportantTypes, NumberOfImportantTypesRequested));
					}
				}

				if (ct.IsCancellationRequested)
				{
					return (null, null);
				}

				if (_statisticsTCS != null)
				{
					_currentUpdateState = UpdateState.AnalyzingFullGraphStatistics;
					if (_nodeGraph == null)
					{
						_statisticsTCS.TrySetResult(null);
					}
					else
					{
						var nodeGraph = _nodeGraph;
						var stats = await Task.Run(() => GraphStatistics.GetForFullGraph(nodeGraph), ct);
						_statisticsTCS.TrySetResult(stats);
					}

					_statisticsTCS = null;
				}

				if (_nodeGraph != null)
				{
					// Include active document ( == selected node)
					_currentUpdateState = UpdateState.IncludeActive;
					if (IncludeActiveMode == IncludeActiveMode.DontInclude)
					{
						ModifySubgraph(Subgraph.ClearSelectedOp());
					}
					else
					{
						var activeDocument = _getActiveDocument();
						compilationCache = EnsureCompilationCache();
						if (activeDocument != null)
						{
							var activeSymbols = await compilationCache.GetDeclaredSymbolsFromFilePath(activeDocument, ct);
							var activeNodeKey =
								// Prefer current selected if it matches active document, otherwise first type in document
								activeSymbols.Select(s => s.ToNodeKey()).FirstOrDefault(k => k == SelectedNode?.Key) ??
								activeSymbols.FirstOrDefault()?.ToNodeKey();
							if (activeNodeKey is not null)
							{
								ModifySubgraph(Subgraph.SetSelectedOp(activeNodeKey, IncludeActiveMode == IncludeActiveMode.ActiveAndConnections));
							}
						}
					}
				}

				if (ct.IsCancellationRequested)
				{
					return (null, null);
				}

				if (_shouldClearUnpinned && _nodeGraph != null)
				{
					// This should be done after all other subgraph operations have been added - this will minimise jitter,
					// if one of those operations would cause a node to stay in the graph that would otherwise be removed
					TryClearCategory(Subgraph.InclusionCategory.Unpinned, false);
				}

				if (_pendingSubgraphOperations.Count > 0 && _nodeGraph != null)
				{
					_currentUpdateState = UpdateState.UpdatingSubgraph;
					var subgraphOperations = _pendingSubgraphOperations.ToList();
					_pendingSubgraphOperations.Clear();

					var nodeGraph = _nodeGraph;

					var includedNodes = _includedNodes;

					var modified = false;
					await Task.Run(async () =>
					{
						modified |= await Subgraph.SanitizeOp().Apply(includedNodes, nodeGraph, ct);
						foreach (var op in subgraphOperations)
						{
							modified |= await op.Apply(includedNodes, nodeGraph, ct);
						}
					}, ct);

					_needsDisplayGraphUpdate |= modified && _includedNodes == includedNodes;
				}

				if (ct.IsCancellationRequested)
				{
					return (null, null);
				}

				if (_needsDisplayGraphUpdate)
				{
					_currentUpdateState = UpdateState.RebuildingDisplayGraph;
					_needsDisplayGraphUpdate = false;

					var displayGraph = await RebuildDisplayGraph(_includedNodes, ct);

					if (ct.IsCancellationRequested)
					{
						return (null, null);
					}
					else
					{
						return displayGraph;
					}
				}
			}
			catch (Exception e)
			{
				if (!ct.IsCancellationRequested)
				{
#if DEBUG
					if (Debugger.IsAttached)
					{
						Debugger.Break();
					}
#endif
					UpdateFailed?.Invoke(e);
				}

				return (null, null); // Don't invoke Unchanged event
			}
			finally
			{
				_currentUpdateState = UpdateState.NotUpdating;
				compilationCache?.ClearSolution();
			}

			if (!ct.IsCancellationRequested)
			{
				UpdateCompletedUnchanged?.Invoke();
			}

			return (null, null); // In case we updated NodeGraph but display graph did not change
		}

		private async Task<NodeGraph?> RebuildNodeGraph(CompilationCache compilationCache, CancellationToken ct)
		{
			var includedProjects = _includedProjects;
			var excludePureGenerated = !IncludePureGenerated;

			while (ShouldWait())
			{
				// Don't bother to build graph while solution is still opening
				await Task.Delay(1000);
			}

			await _graphRebuildIdleTimer.WaitForIdle(ct);

			if (ct.IsCancellationRequested)
			{
				return null;
			}

			try
			{
				if (_outputService.IsEnabled(OutputLevel.Diagnostic))
				{
					_outputService.WriteLine($"Starting graph rebuild, {_backgroundGraphRebuildsActive} other rebuilds already underway");
				}
				_backgroundGraphRebuildsActive++;
				var nodeGraph = await Task.Run(async () =>
				{
					return await NodeGraph.BuildGraph(compilationCache, includedProjects, excludePureGenerated, ct);
				}, ct);
				return nodeGraph;
			}
			finally
			{
				_backgroundGraphRebuildsActive--;
				if (_outputService.IsEnabled(OutputLevel.Diagnostic))
				{
					var concluded = ct.IsCancellationRequested ? "Cancelled" : "Completed";
					_outputService.WriteLine($"{concluded} graph rebuild, {_backgroundGraphRebuildsActive} other rebuilds still underway");
				}
			}

			bool ShouldWait() => !ct.IsCancellationRequested && (_backgroundGraphRebuildsActive > 1 || _isSolutionStillOpening());
		}

		private async Task AnnotateGitInfo(CancellationToken ct)
		{
			if (_nodeGraph is { } nodeGraph)
			{
				var gitInfos = await _getGitInfo(ct);
				if (ct.IsCancellationRequested)
				{
					return;
				}

				var (_, _, removed) = gitInfos.GetUnorderedDiff(_previousGitInfos);

				// build dict of status by filename
				var infoDict = GetDictionary(gitInfos);
				var previousInfoDict = GetDictionary(_previousGitInfos);
				foreach (var info in removed)
				{
					infoDict[info.FullPath] = GitStatus.Unchanged;
				}

				// for each filename, get node(s)
				var affectedNodes = infoDict.Keys.SelectMany(fp => nodeGraph.GetAssociatedNodes(fp));

				// for each node, get combined status from all files
				// set status on node
				// if status is changed, queue up node graph operation
				foreach (var node in affectedNodes)
				{
					var nodeStatus = GetStatusFromInfos(infoDict, node);
					var oldStatus = GetStatusFromInfos(previousInfoDict, node);

					node.GitStatus = nodeStatus;
					if (nodeStatus != oldStatus)
					{
						_needsDisplayGraphUpdate = true;
						var op = nodeStatus == GitStatus.Unchanged ?
							Subgraph.RemoveFromCategoryOp(node.Key, Subgraph.InclusionCategory.GitChanges) :
							Subgraph.AddToCategoryOp(node.Key, Subgraph.InclusionCategory.GitChanges);
						ModifySubgraph(op);
					}
				}

				_previousGitInfos = gitInfos;

				static Dictionary<string, GitStatus> GetDictionary(ICollection<GitInfo>? gitInfos)
				{
					var infoDict = gitInfos.ToDictionary(gi => gi.FullPath, gi => gi.Status);
					return infoDict;
				}

				static GitStatus GetStatusFromInfos(Dictionary<string, GitStatus> infoDict, Node node)
				{
					var infos = node.AssociatedFiles.Select(f => infoDict.GetOrDefault(f));
					var nodeStatus = GitStatus.Unchanged;
					foreach (var status in infos)
					{
						nodeStatus |= status;
					}

					return nodeStatus;
				}
			}
		}

		private async Task<(IBidirectionalGraph<DisplayNode, DisplayEdge>?, GraphStatistics?)> RebuildDisplayGraph(Subgraph includedNodes, CancellationToken ct)
		{
			var nodeGraph = _nodeGraph;
			if (nodeGraph == null)
			{
				return (null, null);
			}
			var result = await Task.Run(() =>
			{
				var graph = nodeGraph.GetDisplaySubgraph(includedNodes, parentContext: _nodeParentContext);
				var stats = GraphStatistics.GetForSubgraph(graph);
				return (graph, stats);
			}, ct);

			return result;
		}

		private void TryClearCategory(Subgraph.InclusionCategory category, bool leaveUnpinned)
		{
			if (_includedNodes.HasNodesInCategory(category))
			{
				var op = leaveUnpinned ?
					Subgraph.ClearCategoryAndLeaveUnpinnedOp(category) :
					Subgraph.ClearCategoryOp(category);
				ModifySubgraph(op);
			}
		}

		private void TryClearCategories(IEnumerable<Subgraph.InclusionCategory> categories, bool leaveUnpinned)
		{
			foreach (var category in categories)
			{
				TryClearCategory(category, leaveUnpinned);
			}
		}

		public void Dispose()
		{
			_updateSubscription.Dispose();
		}

		private enum UpdateState
		{
			// Inactive states
			NotUpdating,
			WaitingForIdle,

			// NodeGraph modifications
			RebuildingNodeGraph,
			UpdatingNodeGraph,

			// Metadata and analyses
			UpdatingGitInfo,
			UpdatingImportantTypes,
			AnalyzingFullGraphStatistics,

			// Update subgraph model
			IncludeActive,
			UpdatingSubgraph,

			// Build final display graph
			RebuildingDisplayGraph,
		}
	}
}
