﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeConnections.Extensions;
using CodeConnections.Roslyn;
using CodeConnections.Utilities;
using Microsoft.CodeAnalysis;

namespace CodeConnections.Graph
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
		private static async Task BuildGraph(NodeGraph graph, CompilationCache compilationCache, IEnumerable<ProjectIdentifier> projects, CancellationToken ct)
		{
			var knownNodes = new Dictionary<TypeIdentifier, TypeNode>();

			foreach (var project in projects)
			{
				var syntaxTrees = await compilationCache.GetSyntaxTreesForProject(project, ct);
				// We use a hashset here because different SyntaxTrees may declare the same symbol (eg partial definitions)
				// Note: it's safe to hash by ITypeSymbol because they're all coming from the same Compilation
				var declaredSymbols = new HashSet<ITypeSymbol>();
				foreach (var syntaxTree in syntaxTrees)
				{
					if (ct.IsCancellationRequested)
					{
						return;
					}
					var root = await syntaxTree.GetRootAsync(ct);
					var semanticModel = await compilationCache.GetSemanticModel(syntaxTree, project, ct);
					if (semanticModel == null)
					{
						continue;
					}
					declaredSymbols.UnionWith(graph.GetIncludedSymbolsFromSyntaxRoot(root, semanticModel));
				}

				foreach (var symbol in declaredSymbols)
				{
					if (ct.IsCancellationRequested)
					{
						return;
					}

					var node = GetFromKnownNodes(symbol, compilationCache);
					await foreach (var dependency in symbol.GetTypeDependencies(compilationCache, project, includeExternalMetadata: false, ct))
					{
						if (graph.IsSymbolIncluded(dependency))
						{
							var dependencyNode = GetFromKnownNodes(dependency, compilationCache);

							if (node != dependencyNode)
							{
								node.AddForwardLink(dependencyNode, GetLinkType(dependency, symbol));
							}
						}
					}
				}

				TypeNode GetFromKnownNodes(ITypeSymbol symbol, CompilationCache cache)
				{
					var identifier = symbol.ToIdentifier();
					if (!knownNodes.TryGetValue(identifier, out var node))
					{
						var key = new TypeNodeKey(identifier);
						node = CreateTypeNodeForSymbol(symbol, key, cache);
						knownNodes[identifier] = node;
						graph.AddNode(node);
					}

					return node;
				}
			}
		}

		private static LinkType GetLinkType(ITypeSymbol dependency, ITypeSymbol dependent)
		{
			var linkType = LinkType.Unspecified;
			// Check ConstructedFrom (eg IFoo<T> instead of IFoo<string>) - t.ConstructedFrom == t for non-generic types
			if (dependency.Equals(dependent.BaseType?.ConstructedFrom, SymbolEqualityComparer.Default))
			{
				linkType |= LinkType.InheritsFromClass;
			}

			if (dependent.Interfaces.Select(i => i.ConstructedFrom).Contains(dependency))
			{
				linkType |= LinkType.ImplementsInterface;
			}

			return linkType;
		}

		private static TypeNode CreateTypeNodeForSymbol(ITypeSymbol symbol, TypeNodeKey key, CompilationCache cache)
		{
			var associatedFiles = GetAssociatedFiles(symbol);
			return new TypeNode(
				key,
				symbol.GetPreferredDeclaration(),
				associatedFiles,
				symbol.GetFullMetadataName(),
				symbol.GetLineCount(),
				isNestedType: symbol.ContainingType != null,
				isPurelyGenerated: symbol.IsPurelyGenerated(),
				symbol.DeclaredAccessibility,
				cache.GetContainingProject(associatedFiles)?.ToIdentifier()
			);
		}

		private static IEnumerable<string> GetAssociatedFiles(ITypeSymbol symbol)
			=> symbol.DeclaringSyntaxReferences.Select(sr => sr.SyntaxTree.FilePath).ToHashSet();

		/// <summary>
		/// Get node for <paramref name="symbol"/>. If none is present, create one and add it to the graph.
		/// </summary>
		private TypeNode GetOrCreateNode(ITypeSymbol symbol, CompilationCache cache)
		{
			var identifier = symbol.ToIdentifier();
			var key = new TypeNodeKey(identifier);
			if (!_nodes.TryGetValue(key, out var node))
			{
				node = CreateTypeNodeForSymbol(symbol, key, cache);
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

			if (_excludePureGenerated && foundSymbol.IsPurelyGenerated())
			{
				return false;
			}

			if (!IncludedTypeKinds.Contains(foundSymbol.TypeKind))
			{
				return false;
			}

			if (!foundSymbol.CanBeReferencedByName)
			{
				// This excludes anonymous types
				return false;
			}

			return true;
		}
	}
}
