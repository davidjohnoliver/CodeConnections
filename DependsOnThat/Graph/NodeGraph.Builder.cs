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
		/// <summary>
		/// Build out the contents of a graph whose roots have been defined.
		/// </summary>
		/// <remarks>
		/// Although all symbols in the solution are considered, no 'master list' of discovered nodes is retained; those nodes that are reachable 
		/// from <see cref="Roots"/> as either dependencies or dependents will be held via forward- and backlink hard references.
		/// </remarks>
		private static async Task BuildGraphFromRoots(NodeGraph graph, Solution solution, CancellationToken ct)
		{
			var knownNodes = graph.Roots.OfType<TypeNode>().ToDictionary(n => n.Symbol);
			//// Note: we run tasks serially here instead of trying to aggressively parallelize, on the grounds that (a) Roslyn is largely single-threaded 
			//// anyway https://softwareengineering.stackexchange.com/a/330028/336780, and (b) the UX we want is a stable ordering of node links, which wouldn't 
			//// be the case if we processed task results out of order.
			foreach (var project in solution.Projects) // A valid potential optimization here would be to first build the inter-project dependency graph, and only include those projects in the connected subgraph(s) that roots.
			{
				var compilation = await project.GetCompilationAsync(ct);
				if (compilation == null)
				{
					continue;
				}

				// We use a hashset here because different SyntaxTrees may declare the same symbol (eg partial definitions)
				var declaredSymbols = new HashSet<ITypeSymbol>();
				foreach (var syntaxTree in compilation.SyntaxTrees)
				{
					if (ct.IsCancellationRequested)
					{
						return;
					}
					var root = await syntaxTree.GetRootAsync(ct);
					var semanticModel = compilation.GetSemanticModel(syntaxTree);
					declaredSymbols.UnionWith(root.GetAllDeclaredTypes(semanticModel));
				}

				foreach (var symbol in declaredSymbols)
				{
					if (ct.IsCancellationRequested)
					{
						return;
					}

					var node = GetFromKnownNodes(symbol);
					// TODO: GetTypeDependencies calls GetSemanticModel() a second time, now that we're always considering all SyntaxTrees it'd be more efficient to refactor to only create it once (eg GetTypeDependenciesForDefinition)
					await foreach (var dependency in symbol.GetTypeDependencies(compilation, includeExternalMetadata: false, ct))
					{
						var dependencyNode = GetFromKnownNodes(dependency);

						node.AddForwardLink(dependencyNode);
					}
				}

				TypeNode GetFromKnownNodes(ITypeSymbol symbol)
				{
					if (!knownNodes.TryGetValue(symbol, out var node))
					{
						node = new TypeNode(symbol, symbol.GetPreferredDeclaration());
						knownNodes[symbol] = node;
					}

					return node;
				}
			}
		}
	}
}
