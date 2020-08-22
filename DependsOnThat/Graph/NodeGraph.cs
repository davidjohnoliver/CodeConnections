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
	}
}
