#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Graph;
using DependsOnThat.Graph.Display;
using DependsOnThat.Presentation;
using DependsOnThat.Utilities;
using QuickGraph;
using QuickGraph.Algorithms.Condensation;

namespace DependsOnThat.Extensions
{
	public static class NodeGraphExtensions
	{
		/// <summary>
		/// Get subgraph ready for display.
		/// </summary>
		/// <param name="extensionDepth">
		/// The depth to extend the subgraph from the <paramref name="rootSymbols"/>. A value of 0 will only include the roots, a value of 1 will 
		/// include 1st-nearest neighbours (both upstream and downstream), etc.
		/// </param>
		public static IBidirectionalGraph<DisplayNode, DisplayEdge> GetDisplaySubgraph(this NodeGraph nodeGraph, IList<(string FilePath, Microsoft.CodeAnalysis.ITypeSymbol Symbol)> rootSymbols, int extensionDepth)
			=> GetDisplaySubgraph(nodeGraph, rootSymbols.Select(tpl => nodeGraph.GetNodeForType(tpl.Symbol)).Trim(), extensionDepth);

		public static IBidirectionalGraph<DisplayNode, DisplayEdge> GetDisplaySubgraph(this NodeGraph nodeGraph, IEnumerable<TypeNode> rootNodes, int extensionDepth)
		{
			var rootNodesSet = rootNodes.ToHashSet();
			var subgraphNodes = nodeGraph.ExtendSubgraphFromRoots(rootNodesSet, extensionDepth);
			var graph = new BidirectionalGraph<DisplayNode, DisplayEdge>();
			var displayNodes = subgraphNodes.ToDictionary(n => n, n => n.ToDisplayNode(n is TypeNode typeNode && rootNodesSet.Contains(typeNode)));
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

			if (rootNodesSet.Count > 1)
			{
				// Add multi-dependency edges, if any applicable
				var rootPaths = GetMultiDependencyRootPaths(nodeGraph, rootNodesSet);
				var pathsToDisplay = rootPaths.Where(p => p.IntermediateLength > extensionDepth * 2); //
				foreach (var path in pathsToDisplay)
				{
					graph.AddEdge(path.ToDisplayEdge(extensionDepth, displayNodes));
				}
			}

			return graph;
		}

		public static IEnumerable<NodePath> GetMultiDependencyRootPaths(NodeGraph graph, HashSet<TypeNode> roots)
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
			var rootNodes = nodeGraph.Nodes.Values.OfType<TypeNode>();
			var fullDisplayGraph = GetDisplaySubgraph(nodeGraph, rootNodes, extensionDepth: 0);
			return (fullDisplayGraph.GetClusteredGraph(), fullDisplayGraph);
		}
	}
}
