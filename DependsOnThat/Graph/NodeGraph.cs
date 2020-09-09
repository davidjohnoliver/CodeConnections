#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using Microsoft.CodeAnalysis;

namespace DependsOnThat.Graph
{
	/// <summary>
	/// A dependency graph for a solution.
	/// </summary>
	public sealed partial class NodeGraph
	{
		private readonly Dictionary<NodeKey, Node> _nodes = new Dictionary<NodeKey, Node>();
		public IReadOnlyDictionary<NodeKey, Node> Nodes => _nodes;

		private NodeGraph()
		{
		}

		private void AddNode(Node node) => _nodes[node.Key] = node;

		public TypeNode? GetNodeForType(ITypeSymbol type) => _nodes.GetOrDefault(new TypeNodeKey(type.ToIdentifier())) as TypeNode;

		public static async Task<NodeGraph?> BuildGraph(Solution solution, CancellationToken ct)
		{
			var graph = new NodeGraph();
			await BuildGraph(graph, solution, ct);
			if (ct.IsCancellationRequested)
			{
				return null;
			}
			return graph;
		}

		/// <summary>
		/// Get a subset of nodes extended from the <paramref name="roots"/> to the nominated depth.
		/// </summary>
		/// <param name="depth">
		/// The depth to extend the subgraph from the <paramref name="roots"/>. A value of 0 will only include the roots, a value of 1 will 
		/// include 1st-nearest neighbours (both upstream and downstream), etc.
		/// </param>
		public HashSet<Node> ExtendSubgraphFromRoots(IEnumerable<Node> roots, int depth)
		{
			var nodes = new HashSet<Node>();
			foreach (var root in roots)
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
