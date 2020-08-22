#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using DependsOnThat.Utilities;
using Microsoft.CodeAnalysis;

namespace DependsOnThat.Graph
{
	public partial class NodeGraph
	{
		private static async Task BuildGraphFromRoots(NodeGraph graph, Solution solution, CancellationToken ct)
		{
			var seen = new Dictionary<ITypeSymbol, TypeNode>();
			var toVisit = new Stack<TypeNode>(graph.Roots.OfType<TypeNode>());

			var lp = new LoopProtection();
			// Note: we run tasks serially here instead of trying to aggressively parallelize, on the grounds that (a) Roslyn is largely single-threaded 
			// anyway https://softwareengineering.stackexchange.com/a/330028/336780, and (b) the UX we want is a stable ordering of node links, which wouldn't 
			// be the case if we processed task results out of order.
			while (toVisit.Count > 0)
			{
				lp.Iterate();
				var current = toVisit.Pop();
				var compilation = await current.Symbol.GetCompilation(solution, ct);
				if (ct.IsCancellationRequested)
				{
					return;
				}
				if (compilation == null)
				{
					continue;
				}
				await foreach (var dependency in current.Symbol.GetTypeDependencies(compilation, includeExternalMetadata: false).WithCancellation(ct))
				{
					if (!seen.TryGetValue(dependency, out var node))
					{
						node = new TypeNode(dependency);
						seen[dependency] = node;
						toVisit.Push(node);
					}

					current.AddForwardLink(node);
				}
			}
		}
	}
}
