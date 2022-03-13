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
			=> GetDisplaySubgraph(nodeGraph, rootSymbols.Select(tpl => nodeGraph.GetNodeForType(tpl)).Trim());

		public static IBidirectionalGraph<DisplayNode, DisplayEdge> GetDisplaySubgraph(this NodeGraph nodeGraph, IEnumerable<Node> subgraphNodes)
		{
			var displayNodes = subgraphNodes.ToDictionary(n => n, n => n.ToDisplayNode(false, false, null));
			return GetDisplaySubgraph(nodeGraph, subgraphNodes, displayNodes);
		}

		public static IBidirectionalGraph<DisplayNode, DisplayEdge> GetDisplaySubgraph(this NodeGraph nodeGraph, Subgraph subgraph, object? parentContext)
		{
			var subgraphNodes = subgraph.AllNodes.Select(k => nodeGraph.Nodes[k]);
			var displayNodes = subgraphNodes.ToDictionary(n => n,
				n => n.ToDisplayNode(
					subgraph.IsPinned(n.Key),
					subgraph.IsInCategory(n.Key, Subgraph.InclusionCategory.ImportantType),
					parentContext
				)
			);
			var graph = GetDisplaySubgraph(nodeGraph, subgraphNodes, displayNodes);

			return graph;
		}

		private static BidirectionalGraph<DisplayNode, DisplayEdge> GetDisplaySubgraph(NodeGraph nodeGraph, IEnumerable<Node> subgraphNodes, Dictionary<Node, DisplayNode> displayNodes)
		{
			var subgraphNodesSet = subgraphNodes.ToHashSet();
			var graph = new BidirectionalGraph<DisplayNode, DisplayEdge>();
			foreach (var kvp in displayNodes)
			{
				graph.AddVertex(kvp.Value);
			}

			foreach (var kvp in displayNodes)
			{
				foreach (var link in kvp.Key.ForwardLinkNodes)
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

		// Find all weakly-connected components, then try to add at least one (shortest) directed path from each component to every other
		public static IEnumerable<NodePath> GetMultiDependencyRootPaths(NodeGraph graph, ISet<Node> roots)
		{

			if (roots.Count < 2)
			{
				return Enumerable.Empty<NodePath>();
			}

			// 1. Find all weakly-connected components
			var unsearched = roots.ToHashSet();
			var toExploreComponents = new Queue<Node>();

			var components = new List<ConnectedComponent>();
			var lp = new LoopProtection();
			while (unsearched.Count > 0)
			{
				lp.Iterate();
				bool TryEnqueue(Node node)
				{
					if (unsearched.Remove(node))
					{
						toExploreComponents.Enqueue(node);
						return true;
					}
					return false;
				}
				var currentComponent = new ConnectedComponent() { Id = components.Count };
				components.Add(currentComponent);
				var current = unsearched.First();
				currentComponent.Add(current);
				TryEnqueue(current);
				while (toExploreComponents.Count > 0)
				{
					lp.Iterate();
					current = toExploreComponents.Dequeue();
					foreach (var neighbour in current.AllLinks())
					{
						if (TryEnqueue(neighbour))
						{
							currentComponent.Add(neighbour);
						}
					}
				}
			}

			if (components.Count < 2)
			{
				return Enumerable.Empty<NodePath>();
			}

			// 2. Find all paths between components
			var toExplore = new Queue<SearchEntry>();
			var nodesSeen = new HashSet<Node>();
			foreach (var component in components)
			{
				foreach (var componentNode in component)
				{
					nodesSeen.Clear();
					toExplore.Enqueue(new SearchEntry(componentNode, null, 0));
					lp = new();
					while (toExplore.Count > 0)
					{
						lp.Iterate();
						var current = toExplore.Dequeue();
						foreach (var link in current.Node.ForwardLinkNodes)
						{
							if (nodesSeen.Contains(link))
							{
								// Already seen
								continue;
							}
							if (component.Contains(link))
							{
								// Part of the same component
								continue;
							}
							if (components.Where(c => c.Contains(link)).FirstOrDefault() is { } targetComponent)
							{
								// We've reached another component - add a path
								nodesSeen.Add(link); // We've found a shortest path for this node-node pair, ignore subsequent paths
								component.AddPath(NodePath.FromSearch(current, link), targetComponent.Id);
								continue;
							}

							// Unseen node that's not part of subgraph - continue searching
							nodesSeen.Add(link);
							toExplore.Enqueue(current.NextGeneration(link));
						}
					}
				}
			}

			return components.SelectMany(c => c.Paths);
		}

		/// <summary>
		/// Generates display and cluster graphs from the full <paramref name="nodeGraph"/>.
		/// </summary>
		public static (IMutableBidirectionalGraph<CyclicCluster, CondensedEdge<DisplayNode, DisplayEdge, CyclicCluster>> Clustered, IBidirectionalGraph<DisplayNode, DisplayEdge> Simple) GetFullClusteredGraph(this NodeGraph nodeGraph)
		{
			var rootNodes = nodeGraph.Nodes.Values;
			var fullDisplayGraph = GetDisplaySubgraph(nodeGraph, rootNodes);
			return (fullDisplayGraph.GetClusteredGraph(), fullDisplayGraph);
		}

		private class ConnectedComponent : HashSet<Node>
		{
			public int Id { get; init; }

			private readonly Dictionary<int, NodePath> _paths = new();
			public IEnumerable<NodePath> Paths => _paths.Values;
			public void AddPath(NodePath path, int targetComponent)
			{
				if (_paths.TryGetValue(targetComponent, out var existing) && existing.Count <= path.Count)
				{
					// New path is no shorter than existing, so keep existing
					return;
				}

				_paths[targetComponent] = path;
			}
		}
	}
}
