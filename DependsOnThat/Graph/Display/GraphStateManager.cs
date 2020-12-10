#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Disposables;
using DependsOnThat.Roslyn;
using Microsoft.CodeAnalysis;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;
using Microsoft.VisualStudio.Threading;
using QuickGraph;
using DependsOnThat.Extensions;
using DependsOnThat.Utilities;
using DependsOnThat.Statistics;

namespace DependsOnThat.Graph.Display
{
	/// <summary>
	/// Manages invalidation and asynchronous updating of <see cref="NodeGraph"/> and display graph state.
	/// </summary>
	public sealed class GraphStateManager : IDisposable
	{
		private int _extensionDepth;
		/// <summary>
		/// N, where the display graph will include the Nth-nearest neighbours of the graph roots.
		/// </summary>
		public int ExtensionDepth
		{
			get => _extensionDepth;
			set
			{
				_extensionDepth = value;
				InvalidateDisplayGraph();
			}
		}

		private bool _excludePureGenerated;
		/// <summary>
		/// Should types defined exclusively in generated code be excluded?
		/// </summary>
		public bool ExcludePureGenerated
		{
			get => _excludePureGenerated;
			set
			{
				_excludePureGenerated = value;
				InvalidateNodeGraph();
			}
		}

		/// <summary>
		/// Raised whenever a new copy of the display graph is created.
		/// </summary>
		public event Action<IBidirectionalGraph<DisplayNode, DisplayEdge>, GraphStatistics>? DisplayGraphChanged;

		private UpdateState _currentUpdateState = UpdateState.NotUpdating;
		private readonly SerialCancellationDisposable _updateSubscription = new SerialCancellationDisposable();
		private readonly IdleTimer _idleTimer = new IdleTimer(idleWaitTimeMS: 2000);

		private ProjectIdentifier[]? _includedProjects;
		private NodeGraph? _nodeGraph;
		/// <summary>
		/// Documents that need to be updated.
		/// </summary>
		private readonly HashSet<DocumentId> _invalidatedDocuments = new HashSet<DocumentId>();
		private readonly JoinableTaskFactory _joinableTaskFactory;
		private readonly Func<Solution> _getCurrentSolution;
		private readonly Func<CancellationToken, Task<IList<ITypeSymbol>>> _getCurrentRootSymbols;
		private bool _needsDisplayGraphUpdate;

		private TaskCompletionSource<GraphStatistics?>? _statisticsTCS;

		public GraphStateManager(JoinableTaskFactory joinableTaskFactory, Func<Solution> getCurrentSolution, Func<CancellationToken, Task<IList<ITypeSymbol>>> getCurrentRootSymbols)
		{
			_joinableTaskFactory = joinableTaskFactory;
			_getCurrentSolution = getCurrentSolution ?? throw new ArgumentNullException(nameof(getCurrentSolution));
			_getCurrentRootSymbols = getCurrentRootSymbols ?? throw new ArgumentNullException(nameof(getCurrentRootSymbols));
		}

		/// <summary>
		/// Invalidate the current <see cref="NodeGraph"/> completely, requiring it to be rebuilt.
		/// </summary>
		public void InvalidateNodeGraph()
		{
			ThreadUtils.ThrowIfNotOnUIThread();

			_invalidatedDocuments.Clear();
			_nodeGraph = null;
			RunUpdate(waitForIdle: false);
		}

		/// <summary>
		/// Invalidate the document corresponding to <paramref name="documentId"/>, triggering a partial update of the <see cref="NodeGraph"/>.
		/// </summary>
		public void InvalidateDocument(DocumentId documentId)
		{
			ThreadUtils.ThrowIfNotOnUIThread();

			_invalidatedDocuments.Add(documentId);

			_idleTimer.RecordActive();

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
		public void InvalidateDisplayGraph()
		{
			ThreadUtils.ThrowIfNotOnUIThread();

			_needsDisplayGraphUpdate = true;

			if (_currentUpdateState is > UpdateState.NotUpdating and < UpdateState.RebuildingDisplayGraph)
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
		/// Set the projects in the solution that should be included in the graph. If null, all projects will be included.
		/// </summary>
		public void SetIncludedProjects(IEnumerable<ProjectIdentifier>? includedProjects)
		{
			_includedProjects = includedProjects?.ToArray();
			InvalidateNodeGraph();
		}

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
		/// Synchronous entry point for the main update routine.
		/// </summary>
		/// <param name="waitForIdle">If true, the update should be delayed until after an idle state (no user input) is reached. If false, it should run immediately.</param>
		private void RunUpdate(bool waitForIdle)
		{
			ThreadUtils.ThrowIfNotOnUIThread();

			if (_currentUpdateState == UpdateState.WaitingForIdle)
			{
				if (!waitForIdle)
				{
					_idleTimer.FastTrackTimer();
				}

				// An update is already running and waiting for idle, nothing to do
				return;
			}

			var ct = _updateSubscription.GetNewToken();
			_joinableTaskFactory.RunAsync(async () =>
			{

				var displayGraph = await Update(waitForIdle, ct);

				if (!ct.IsCancellationRequested && displayGraph.Graph != null && displayGraph.Stats != null)
				{
					DisplayGraphChanged?.Invoke(displayGraph.Graph, displayGraph.Stats);
				}

				if (ct.IsCancellationRequested)
				{
					return;
				}
				else if (_statisticsTCS != null) // This can happen if stats log was requested while building display graph
				{
					RunUpdate(waitForIdle: false);
				}
				// Run another update if we need incremental NodeGraph update
				else if (_invalidatedDocuments.Count > 0)
				{
					RunUpdate(waitForIdle: true);
				}
			});
		}

		/// <summary>
		/// The main update routine. This consists of different phases (as defined by <see cref="UpdateState"/>) which will be run as needed, as determined when the phase is reached.
		/// 
		/// There should only ever be at most one active (ie not cancelled) Update in flight at one time. We can enforce this because the entry point is only ever called on the main thread.
		/// </summary>
		/// <param name="waitForIdle">
		/// Whether the update should wait for an idle state before proceeding. 
		/// 
		/// Even when true, this may be interrupted by subsequent invalidations that require an immediate update.
		/// </param>
		/// <returns>The updated display graph if it is rebuilt, or null otherwise.</returns>
		private async Task<(IBidirectionalGraph<DisplayNode, DisplayEdge>? Graph, GraphStatistics? Stats)> Update(bool waitForIdle, CancellationToken ct)
		{
#pragma warning disable VSTHRD109 // Switch instead of assert in async methods - We're asserting an internal invariant here
			ThreadUtils.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD109 // Switch instead of assert in async methods
			try
			{
				if (waitForIdle)
				{
					_currentUpdateState = UpdateState.WaitingForIdle;
					await _idleTimer.WaitForIdle(ct);
				}

				if (_nodeGraph == null)
				{
					_currentUpdateState = UpdateState.RebuildingDisplayGraph;

					var solution = _getCurrentSolution();
					var nodeGraph = await RebuildNodeGraph(solution, ct);
					if (!ct.IsCancellationRequested)
					{
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
					var solution = _getCurrentSolution();
					var alteredNodes = await _nodeGraph.Update(solution, invalidatedDocuments, ct);
					_needsDisplayGraphUpdate |= alteredNodes?.Count > 0;
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
						var stats = await Task.Run(() => GraphStatistics.GetForFullGraph(_nodeGraph), ct);
						_statisticsTCS.TrySetResult(stats);
					}

					_statisticsTCS = null;
				}

				if (_needsDisplayGraphUpdate)
				{
					_currentUpdateState = UpdateState.RebuildingDisplayGraph;
					_needsDisplayGraphUpdate = false;

					var displayGraph = await RebuildDisplayGraph(ct);

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
			finally
			{
				_currentUpdateState = UpdateState.NotUpdating;

			}

			return (null, null); // In case we updated NodeGraph but display graph did not change
		}

		private async Task<NodeGraph?> RebuildNodeGraph(Solution solution, CancellationToken ct)
		{
			var includedProjects = _includedProjects;
			var excludePureGenerated = ExcludePureGenerated;
			var nodeGraph = await Task.Run(async () =>
			{
				return await NodeGraph.BuildGraph(solution, includedProjects, excludePureGenerated, ct);
			}, ct);
			return nodeGraph;
		}

		private async Task<(IBidirectionalGraph<DisplayNode, DisplayEdge>?, GraphStatistics?)> RebuildDisplayGraph(CancellationToken ct)
		{
			var nodeGraph = _nodeGraph;
			var extensionDepth = ExtensionDepth;
			if (nodeGraph == null)
			{
				return (null, null);
			}
			var result = await Task.Run(async () =>
			{
				var rootSymbols = await _getCurrentRootSymbols(ct);

				var graph = nodeGraph.GetDisplaySubgraph(rootSymbols, extensionDepth);
				var stats = GraphStatistics.GetForSubgraph(graph);
				return (graph, stats);
			}, ct);

			return result;
		}

		public void Dispose()
		{
			_updateSubscription.Dispose();
		}

		private enum UpdateState
		{
			NotUpdating,
			WaitingForIdle,
			RebuildingNodeGraph,
			UpdatingNodeGraph,
			AnalyzingFullGraphStatistics,
			RebuildingDisplayGraph,
		}
	}
}
