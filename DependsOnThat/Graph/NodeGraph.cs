#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using DependsOnThat.Roslyn;
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

		public static async Task<NodeGraph?> BuildGraph(Solution solution, IEnumerable<ProjectIdentifier>? includedProjects = null, bool excludePureGenerated = false, CancellationToken ct = default)
		{
			var graph = new NodeGraph();
			await BuildGraph(graph, solution, includedProjects, excludePureGenerated, ct);
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
			var nodes = new HashSet<Node>(roots);
			if (depth > 0)
			{
				var queue = new Queue<ExtensionSearchEntry>(roots.Select(n => new ExtensionSearchEntry(n, 0)));

				while (queue.Count > 0)
				{
					var next = queue.Dequeue();
					foreach (var link in next.Node.AllLinks())
					{
						// Add connections of all nodes being explored. If the connection's depth would be less than the desired extension 
						// depth, queue it up to be explored (and have its own connections added)
						if (nodes.Add(link) && next.Depth < (depth - 1))
						{
							queue.Enqueue(next.Child(link));
						}
					}
				}
			}
			return nodes;
		}

		private class ExtensionSearchEntry
		{
			public ExtensionSearchEntry(Node node, int depth)
			{
				Node = node;
				Depth = depth;
			}

			public Node Node { get; }
			public int Depth { get; }

			public ExtensionSearchEntry Child(Node childNode) => new ExtensionSearchEntry(childNode, Depth + 1);
		}
	}
}
