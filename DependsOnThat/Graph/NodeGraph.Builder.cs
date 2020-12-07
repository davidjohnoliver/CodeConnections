#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using DependsOnThat.Roslyn;
using DependsOnThat.Utilities;
using Microsoft.CodeAnalysis;

namespace DependsOnThat.Graph
{
	public partial class NodeGraph
	{
		/// <summary>
		/// Build out the contents of a graph for a given solution.
		/// </summary>
		/// <param name="includedProjects">Solution projects to include in the graph. If null, all projects will be included.</param>
		/// <param name="excludePureGenerated">
		/// If true, types that are only declared in generated code will be ignored (types with both authored and generated declarations 
		/// will be treated normally).
		/// </param>
		private static async Task BuildGraph(NodeGraph graph, Solution solution, IEnumerable<Project> projects, CancellationToken ct)
		{
			var knownNodes = new Dictionary<TypeIdentifier, TypeNode>();

			//// Note: we run tasks serially here instead of trying to aggressively parallelize, on the grounds that (a) Roslyn is largely single-threaded 
			//// anyway https://softwareengineering.stackexchange.com/a/330028/336780, and (b) the UX we want is a stable ordering of node links, which wouldn't 
			//// be the case if we processed task results out of order.
			foreach (var project in projects)
			{
				var compilation = await project.GetCompilationAsync(ct);
				if (compilation == null)
				{
					continue;
				}

				// We use a hashset here because different SyntaxTrees may declare the same symbol (eg partial definitions)
				// Note: it's safe to hash by ITypeSymbol because they're all coming from the same Compilation
				var declaredSymbols = new HashSet<ITypeSymbol>();
				foreach (var syntaxTree in compilation.SyntaxTrees)
				{
					if (ct.IsCancellationRequested)
					{
						return;
					}
					var root = await syntaxTree.GetRootAsync(ct);
					var semanticModel = compilation.GetSemanticModel(syntaxTree);
					declaredSymbols.UnionWith(graph.GetIncludedSymbolsFromSyntaxRoot(root, semanticModel));
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
						if (graph.IsSymbolIncluded(dependency))
						{
							var dependencyNode = GetFromKnownNodes(dependency);

							if (node != dependencyNode)
							{
								node.AddForwardLink(dependencyNode);
							}
						}
					}
				}

				TypeNode GetFromKnownNodes(ITypeSymbol symbol)
				{
					var identifier = symbol.ToIdentifier();
					if (!knownNodes.TryGetValue(identifier, out var node))
					{
						var key = new TypeNodeKey(identifier);
						node = CreateTypeNodeForSymbol(symbol, key);
						knownNodes[identifier] = node;
						graph.AddNode(node);
					}

					return node;
				}
			}
		}

		private static TypeNode CreateTypeNodeForSymbol(ITypeSymbol symbol, TypeNodeKey key)
			=> new TypeNode(key, symbol.GetPreferredDeclaration(), GetAssociatedFiles(symbol), symbol.GetFullMetadataName());

		private static IEnumerable<string> GetAssociatedFiles(ITypeSymbol symbol)
			=> symbol.DeclaringSyntaxReferences.Select(sr => sr.SyntaxTree.FilePath).ToHashSet();

		/// <summary>
		/// Get node for <paramref name="symbol"/>. If none is present, create one and add it to the graph.
		/// </summary>
		private TypeNode GetOrCreateNode(ITypeSymbol symbol)
		{
			var identifier = symbol.ToIdentifier();
			var key = new TypeNodeKey(identifier);
			if (!_nodes.TryGetValue(key, out var node))
			{
				node = CreateTypeNodeForSymbol(symbol, key);
				AddNode(node);
			}
			return (TypeNode)node;
		}

		/// <summary>
		/// Get symbols for types declared in <paramref name="root"/> that meet the inclusion rules for the graph.
		/// </summary>
		private IEnumerable<ITypeSymbol> GetIncludedSymbolsFromSyntaxRoot(SyntaxNode root, SemanticModel semanticModel)
			=> root.GetAllDeclaredTypes(semanticModel).Where(foundSymbol => IsSymbolIncluded(foundSymbol));

		/// <summary>
		/// Should this symbol be included in the graph, based on configured inclusion rules?
		/// </summary>
		private bool IsSymbolIncluded(ITypeSymbol foundSymbol)
		{
			if (!_includedAssemblies.Contains(foundSymbol.ContainingAssembly.Name))
			{
				return false;
			}

			if (_excludePureGenerated && foundSymbol.IsPurelyGeneratedSymbol())
			{
				return false;
			}

			return true;
		}
	}
}
