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
		private static async Task BuildGraph(NodeGraph graph, Solution solution, IEnumerable<ProjectIdentifier>? includedProjects, CancellationToken ct)
		{
			var knownNodes = new Dictionary<TypeIdentifier, TypeNode>();

			var projects = includedProjects?.Select(pi => solution.GetProject(pi.Id)).Trim() ?? solution.Projects;

			var includedAssemblies = projects.Select(p => p.AssemblyName).ToHashSet();

			bool IsSymbolIncluded(ITypeSymbol foundSymbol) => includedAssemblies.Contains(foundSymbol.ContainingAssembly.Name);

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
						if (IsSymbolIncluded(dependency))
						{
							var dependencyNode = GetFromKnownNodes(dependency);

							node.AddForwardLink(dependencyNode); 
						}
					}
				}

				TypeNode GetFromKnownNodes(ITypeSymbol symbol)
				{
					var identifier = symbol.ToIdentifier();
					if (!knownNodes.TryGetValue(identifier, out var node))
					{
						node = new TypeNode(identifier, symbol.GetPreferredDeclaration());
						knownNodes[identifier] = node;
						graph.AddNode(node);
					}

					return node;
				}
			}
		}
	}
}
