#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeConnections.Extensions;
using CodeConnections.Roslyn;
using Microsoft.CodeAnalysis;

namespace CodeConnections.Graph
{
	/// <summary>
	/// A dependency graph for a solution.
	/// </summary>
	public sealed partial class NodeGraph
	{
		private readonly Dictionary<NodeKey, Node> _nodes = new Dictionary<NodeKey, Node>();
		public IReadOnlyDictionary<NodeKey, Node> Nodes => _nodes;

		/// <summary>
		/// Indexes all nodes associated with a given document (by filepath), per the <see cref="Node.AssociatedFiles"/> property.
		/// </summary>
		private readonly Dictionary<string, List<Node>> _nodesByDocument = new Dictionary<string, List<Node>>();
		/// <summary>
		/// Should types that are declared only in generated code be excluded from the graph?
		/// </summary>
		private readonly bool _excludePureGenerated;
		/// <summary>
		/// Set of all assemblies that should be included in the graph.
		/// </summary>
		private readonly HashSet<string> _includedAssemblies;

		private static readonly ICollection<TypeKind> IncludedTypeKinds = new[] { TypeKind.Class, TypeKind.Interface, TypeKind.Enum, TypeKind.Struct };

		private NodeGraph(bool excludePureGenerated, IEnumerable<string> includedAssemblies)
		{
			_excludePureGenerated = excludePureGenerated;
			_includedAssemblies = includedAssemblies.ToHashSet();
		}

		private void AddNode(Node node)
		{
			_nodes[node.Key] = node;
			foreach (var file in node.AssociatedFiles)
			{
				AddAssociatedFile(node, file);
			}
		}

		private void AddAssociatedFile(Node node, string file)
		{
			_nodesByDocument
				.GetOrCreate(file, _ => new List<Node>(1 /*In the most common case we expect types:documents to be 1:1*/))
				.Add(node);
		}

		private void RemoveAssociatedFile(Node node, string file)
		{
			if (_nodesByDocument.TryGetValue(file, out var associatedNodes))
			{
				associatedNodes.Remove(node);
				if (associatedNodes.Count == 0)
				{
					_nodesByDocument.Remove(file);
				}
			}
		}

		public TypeNode? GetNodeForType(ITypeSymbol type) => _nodes.GetOrDefault(type.ToNodeKey()) as TypeNode;

		public static async Task<NodeGraph?> BuildGraph(CompilationCache compilationCache, IEnumerable<ProjectIdentifier>? includedProjects = null, bool excludePureGenerated = false, CancellationToken ct = default)
		{
			var projects = includedProjects ?? compilationCache.GetAllProjects();

			var includedAssemblies = projects.Select(p => compilationCache.GetAssemblyName(p)).Trim();

			var graph = new NodeGraph(excludePureGenerated, includedAssemblies);

			await BuildGraph(graph, compilationCache, projects, ct);
			if (ct.IsCancellationRequested)
			{
				return null;
			}
			return graph;
		}

		/// <summary>
		/// Get all nodes in the graph associated with file <paramref name="filePath"/>
		/// </summary>
		public IEnumerable<Node> GetAssociatedNodes(string? filePath)
		{
			if (filePath == null)
			{
				return Enumerable.Empty<Node>();
			}

			return _nodesByDocument.GetOrDefault(filePath) ?? Enumerable.Empty<Node>();
		}
	}
}
