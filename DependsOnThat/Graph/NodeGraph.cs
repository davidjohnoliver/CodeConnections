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

		public static async Task<NodeGraph?> BuildGraph(Solution solution, IEnumerable<ProjectIdentifier>? includedProjects = null, bool excludePureGenerated = false, CancellationToken ct = default)
		{
			var projects = includedProjects?.Select(pi => solution.GetProject(pi.Id)).Trim() ?? solution.Projects;

			var includedAssemblies = projects.Select(p => p.AssemblyName);

			var graph = new NodeGraph(excludePureGenerated, includedAssemblies);

			await BuildGraph(graph, solution, projects, ct);
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
