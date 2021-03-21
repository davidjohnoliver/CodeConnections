#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Graph;
using CodeConnections.Graph.Display;
using CodeConnections.Presentation;
using CodeConnections.Utilities;
using Microsoft.CodeAnalysis;
using QuickGraph;
using QuickGraph.Algorithms.Condensation;

namespace CodeConnections.Extensions
{
	public static class NodeGraphExtensions
	{
		/// <summary>
		/// Get subgraph ready for display.
		/// </summary>
		public static IBidirectionalGraph<DisplayNode, DisplayEdge> GetDisplaySubgraph(this NodeGraph nodeGraph, IList<ITypeSymbol> rootSymbols)
			=> GetDisplaySubgraph(nodeGraph, rootSymbols.Select(tpl => nodeGraph.GetNodeForType(tpl)).Trim(), SetUtils.GetEmpty<NodeKey>());

		public static IBidirectionalGraph<DisplayNode, DisplayEdge> GetDisplaySubgraph(this NodeGraph nodeGraph, IEnumerable<Node> subgraphNodes, ISet<NodeKey> pinnedNodes, object? parentContext = null)
		{
			var subgraphNodesSet = subgraphNodes.ToHashSet();
			var graph = new BidirectionalGraph<DisplayNode, DisplayEdge>();
			var displayNodes = subgraphNodesSet.ToDictionary(n => n, n => n.ToDisplayNode(pinnedNodes.Contains(n.Key), parentContext));
			foreach (var kvp in displayNodes)
			{
				graph.AddVertex(kvp.Value);
			}

			foreach (var kvp in displayNodes)
			{
				foreach (var link in kvp.Key.ForwardLinks)
				{
					if (displayNodes.ContainsKey(link))
					{
						// Add dependencies as edges if both ends are part of the subgraph
						graph.AddEdge(new SimpleDisplayEdge(kvp.Value, displayNodes[link]));
					}
				}
			}

			if (subgraphNodesSet.Count > 1)
			{
				// Add multi-dependency edges, if any applicable
				var rootPaths = GetMultiDependencyRootPaths(nodeGraph, subgraphNodesSet);
				var pathsToDisplay = rootPaths.Where(p => p.IntermediateLength > 0); //
				foreach (var path in pathsToDisplay)
				{
					graph.AddEdge(path.ToDisplayEdge(displayNodes));
				}
			}

			return graph;
		}

		public static IEnumerable<NodePath> GetMultiDependencyRootPaths(NodeGraph graph, ISet<Node> roots)
		{
			if (roots.Count < 2)
			{
				throw new ArgumentOutOfRangeException(nameof(roots));
			}

			var toExplore = new Queue<SearchEntry>();
			var nodesSeen = new HashSet<Node>();
			foreach (var root in roots)
			{
				toExplore.Clear();
				nodesSeen.Clear();
				var rootsNotFound = roots.Count - 1;
				toExplore.Enqueue(new SearchEntry(root, previous: null, generation: 0));

				var lp = new LoopProtection();
				while (toExplore.Count > 0)
				{
					lp.Iterate();

					var current = toExplore.Dequeue();

					foreach (var link in current.Node.ForwardLinks)
					{
						if (nodesSeen.Contains(link))
						{
							// Seen before, is either in queue or already explored (or is a root)
							continue;
						}

						// Mark as seen
						nodesSeen.Add(link);

						if (roots.Contains(link) && link != root)
						{
							// We've found another root
							yield return NodePath.FromSearch(current, link);

							rootsNotFound--;

							// Note: we don't add it to the explore queue, because we don't want paths with more than 2 roots. Any connections out of that root 
							// will be found when that root is processed.
						}
						else
						{
							// Regular node we haven't seen - queue it up to explore
							toExplore.Enqueue(current.NextGeneration(link));
						}

						if (rootsNotFound == 0)
						{
							// All roots have been found for this root, go to the next one
							break;
						}
					}

					if (rootsNotFound == 0)
					{
						// All roots have been found for this root, go to the next one
						break;
					}
				}
			}
		}

		/// <summary>
		/// Generates display and cluster graphs from the full <paramref name="nodeGraph"/>.
		/// </summary>
		public static (IMutableBidirectionalGraph<CyclicCluster, CondensedEdge<DisplayNode, DisplayEdge, CyclicCluster>> Clustered, IBidirectionalGraph<DisplayNode, DisplayEdge> Simple) GetFullClusteredGraph(this NodeGraph nodeGraph)
		{
			var rootNodes = nodeGraph.Nodes.Values;
			var fullDisplayGraph = GetDisplaySubgraph(nodeGraph, rootNodes, SetUtils.GetEmpty<NodeKey>());
			return (fullDisplayGraph.GetClusteredGraph(), fullDisplayGraph);
		}
	}
}
