#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace DependsOnThat.Graph
{
	/// <summary>
	/// A dependency graph defined by one or more roots (which may be connected or disconnected).
	/// </summary>
	public sealed partial class NodeGraph
	{
		/// <summary>
		/// The root nodes of the graph.
		/// </summary>
		public IReadOnlyList<Node> Roots { get; }
		private NodeGraph(IEnumerable<Node> roots)
		{
			Roots = new List<Node>(roots);
			foreach (var node in Roots)
			{
				node.IsRoot = true;
			}
		}

		public static Task<NodeGraph?> BuildGraphFromRoots(IEnumerable<ITypeSymbol> roots, Solution solution, CancellationToken ct)
			=> BuildGraphFromRoots(roots.Select(s => new TypeNode(s)), solution, ct);

		public static async Task<NodeGraph?> BuildGraphFromRoots(IEnumerable<Node> roots, Solution solution, CancellationToken ct)
		{
			var graph = new NodeGraph(roots);
			await BuildGraphFromRoots(graph, solution, ct);
			if (ct.IsCancellationRequested)
			{
				return null;
			}
			return graph;
		}

		/// <summary>
		/// Get a subset of nodes extended from the roots to the nominated depth.
		/// </summary>
		/// <param name="depth">
		/// The depth to extend the subgraph from the <see cref="NodeGraph.Roots"/>. A value of 0 will only include the roots, a value of 1 will 
		/// include 1st-nearest neighbours (both upstream and downstream), etc.
		/// </param>
		public HashSet<Node> ExtendSubgraphFromRoots(int depth)
		{
			var nodes = new HashSet<Node>();
			foreach (var root in Roots)
			{
				ExploreNode(root, depth);

				void ExploreNode(Node node, int currentDepth)
				{
					if (currentDepth < 0)
					{
						return;
					}

					if (!nodes.Contains(node))
					{
						nodes.Add(node);

						foreach (var link in node.ForwardLinks)
						{
							ExploreNode(link, currentDepth - 1);
						}

						foreach (var link in node.BackLinks)
						{
							ExploreNode(link, currentDepth - 1);
						}
					}
				}
			}
			return nodes;
		}
	}
}
