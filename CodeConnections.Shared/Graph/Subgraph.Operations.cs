#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeConnections.Extensions;
using CodeConnections.Utilities;

namespace CodeConnections.Graph
{
	public partial class Subgraph
	{
		/// <summary>
		/// An operation to add <paramref name="nodesToAdd"/> as <see cref="PinnedNodes"/> to a subgraph.
		/// </summary>
		public static Operation AddPinned(params NodeKey[] nodesToAdd) => new AddPinnedOperation(nodesToAdd);

		/// <summary>
		/// An operation to add <paramref name="node"/> to <paramref name="category"/>.
		/// </summary>
		public static Operation AddToCategory(NodeKey node, InclusionCategory category) => new AddToCategoryOperation(node, category);

		/// <summary>
		/// An operation to remove <paramref name="node"/> from <paramref name="categoryToRemove"/>.
		/// </summary>
		public static Operation RemoveFromCategory(NodeKey node, InclusionCategory categoryToRemove) => new RemoveFromCategoryOperation(node, categoryToRemove);

		public static Operation ClearCategory(InclusionCategory category) => new ClearCategoryOperation(category);

		/// <summary>
		/// An operation to remove all nodes in <paramref name="category"/> from that category, but leave them in the subgraph
		/// in an <see cref="InclusionCategory.Unpinned"/> loose state.
		/// </summary>
		public static Operation ClearCategoryAndLeaveUnpinned(InclusionCategory category) => new ClearCategoryAndLeaveUnpinnedOperation(category);

		/// <summary>
		/// An operation to set a particular node as 'selected', adding it and its neighbours as <see cref="AdditionalNodes"/>.
		/// </summary>

		public static Operation SetSelected(NodeKey selected, bool includeConnectionsAsWell) => new UpdateSelectedOperation(selected, includeConnectionsAsWell);

		public static Operation ClearSelected() => new ClearSelectedOperation();

		/// <summary>
		/// An operation to pin <paramref name="targetNode"/> and all neighbouring nodes to the graph.
		/// </summary>
		public static Operation PinNodeAndNeighbours(NodeKey targetNode) => new PinNodeAndNeighboursOperation(targetNode);

		public static Operation AddInheritanceDependencyHierarchy(NodeKey rootNode)
			=> new AddDependencyOrDependentHierarchyOperation(rootNode, false, LinkType.InheritsOrImplements);

		public static Operation AddInheritanceDependentHierarchy(NodeKey rootNode)
			=> new AddDependencyOrDependentHierarchyOperation(rootNode, true, LinkType.InheritsOrImplements);

		public static Operation AddDirectInheritanceDependents(NodeKey rootNode) => new AddDirectDependenciesOrDependentsOperation(rootNode, true, LinkType.InheritsOrImplements);

		public static Operation AddAllInSameProject(NodeKey rootNode) => new AddAllInSameProjectOperation(rootNode);

		public static Operation AddAllInSolution(NodeKey _) => new AddAllInSolutionOperation();

		/// <summary>
		/// An operation to 'sanitize' the subgraph by removing any nodes that aren't found in the full <see cref="NodeGraph"/>.
		/// </summary>
		public static Operation Sanitize() => new SanitizeOperation();

		private static Operation GetCompositeOperation(params Operation[] operations) => new CompositeOperation(operations);

		/// <summary>
		/// Holds details of an operation to be carried out upon a <see cref="Subgraph"/>.
		/// </summary>
		public abstract class Operation
		{
			/// <summary>
			/// Applies this operation to <paramref name="subgraph"/>.
			/// </summary>
			/// <param name="fullGraph">The full <see cref="NodeGraph"/> set.</param>
			/// <returns>True if <paramref name="subgraph"/> was modified by the operation, false otherwise.</returns>

			public abstract Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct); // TODO: there's no longer a compelling reason for this to be asynchronous
		}

		private abstract class SyncOperation : Operation
		{
			public sealed override Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				var modified = Apply(subgraph, fullGraph);
				return Task.FromResult(modified);
			}

			protected abstract bool Apply(Subgraph subgraph, NodeGraph fullGraph);
		}

		private class CompositeOperation : Operation
		{
			private readonly IList<Operation> _operations;

			public CompositeOperation(IList<Operation> operations)
			{
				_operations = operations;
			}

			public override async Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				var modified = false;

				foreach (var op in _operations)
				{
					modified |= await op.Apply(subgraph, fullGraph, ct);
				}

				return modified;
			}
		}

		private class AddPinnedOperation : Operation
		{
			private readonly IList<NodeKey> _nodeKeys;

			public AddPinnedOperation(IList<NodeKey> nodeKeys)
			{
				_nodeKeys = nodeKeys;
			}

			public override Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				var modified = false;
				foreach (var node in _nodeKeys)
				{
					if (fullGraph.Nodes.ContainsKey(node))
					{
						modified |= subgraph.AddPinnedNode(node, fullGraph);
					}
				}

				return Task.FromResult(modified);
			}
		}

		private class AddToCategoryOperation : Operation
		{
			private readonly NodeKey _node;
			private readonly InclusionCategory _category;

			public AddToCategoryOperation(NodeKey node, InclusionCategory category)
			{
				_node = node;
				_category = category;
			}
			public override Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				var modified = subgraph.AddNodeUnderCategory(_node, _category, fullGraph);
				return Task.FromResult(modified.AddedToSubgraph);
			}
		}

		private class RemoveFromCategoryOperation : Operation
		{
			private readonly NodeKey _node;
			private readonly InclusionCategory _category;

			public RemoveFromCategoryOperation(NodeKey node, InclusionCategory category)
			{
				_node = node;
				_category = category;
			}
			public override Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				var modified = subgraph.RemoveNodeFromCategory(_node, _category);
				return Task.FromResult(modified.RemovedFromSubgraph);
			}
		}

		private class ClearCategoryOperation : Operation
		{
			private readonly InclusionCategory _category;

			public ClearCategoryOperation(InclusionCategory category)
			{
				_category = category;
			}

			public override Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				var modified = false;
				foreach (var node in subgraph.GetNodesForCategory(_category))
				{
					var result = subgraph.RemoveNodeFromCategory(node, _category);
					modified |= result.RemovedFromSubgraph;
				}

				return Task.FromResult(modified);
			}
		}

		private class ClearCategoryAndLeaveUnpinnedOperation : Operation
		{
			private readonly InclusionCategory _category;

			public ClearCategoryAndLeaveUnpinnedOperation(InclusionCategory category)
			{
				_category = category;
			}

			public override Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				foreach (var node in subgraph.GetNodesForCategory(_category))
				{
					subgraph.RemoveNodeFromCategoryAndLeaveUnpinned(node, _category);
				}

				// This will never actually change the population of the subgraph
				return Task.FromResult(false);
			}
		}

		private class UpdateSelectedOperation : Operation
		{
			private readonly NodeKey _selected;
			private readonly bool _includeConnectionsAsWell;

			public UpdateSelectedOperation(NodeKey selected, bool includeConnectionsAsWell)
			{
				_selected = selected ?? throw new ArgumentNullException(nameof(selected));
				_includeConnectionsAsWell = includeConnectionsAsWell;
			}

			public override Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				var modified = false;

				var oldNeighbours = subgraph.GetNodesForCategory(InclusionCategory.NeighbourOfSelected);
				var oldSelected = subgraph.GetNodesForCategory(InclusionCategory.Selected);

				var newSelectedNode = fullGraph.Nodes.GetOrDefaultFromReadOnly(_selected);
				var newNeighbours = newSelectedNode != null && _includeConnectionsAsWell ?
					newSelectedNode.AllLinkKeys(false).ToHashSet() :
					SetUtils.GetEmpty<NodeKey>();


				bool modifiedGraph;
				bool modifiedSelection;
				if (newSelectedNode != null)
				{
					(modifiedGraph, modifiedSelection) = subgraph.AddNodeUnderCategory(_selected, InclusionCategory.Selected, fullGraph);
					modified |= modifiedGraph || modifiedSelection;
				}

				foreach (var neighbour in newNeighbours)
				{
					(modifiedGraph, _) = subgraph.AddNodeUnderCategory(neighbour, InclusionCategory.NeighbourOfSelected, fullGraph);
					modified |= modifiedGraph;
				}

				foreach (var old in oldSelected)
				{
					if (old != _selected)
					{
						(modifiedGraph, modifiedSelection) = subgraph.RemoveNodeFromCategory(old, InclusionCategory.Selected);
						modified |= modifiedGraph || modifiedSelection;
					}
				}

				foreach (var oldNeighbour in oldNeighbours)
				{
					if (!newNeighbours.Contains(oldNeighbour))
					{
						(modifiedGraph, _) = subgraph.RemoveNodeFromCategory(oldNeighbour, InclusionCategory.NeighbourOfSelected);
						modified |= modifiedGraph;
					}
				}

				return Task.FromResult(modified);
			}
		}

		private class ClearSelectedOperation : Operation
		{
			public override Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				var modified = false;
				var oldNeighbours = subgraph.GetNodesForCategory(InclusionCategory.NeighbourOfSelected);
				var oldSelected = subgraph.GetNodesForCategory(InclusionCategory.Selected);


				bool modifiedGraph;
				foreach (var old in oldSelected)
				{
					bool modifiedSelection;
					(modifiedGraph, modifiedSelection) = subgraph.RemoveNodeFromCategory(old, InclusionCategory.Selected);
					modified |= modifiedGraph || modifiedSelection;
				}

				foreach (var oldNeighbour in oldNeighbours)
				{
					(modifiedGraph, _) = subgraph.RemoveNodeFromCategory(oldNeighbour, InclusionCategory.NeighbourOfSelected);
					modified |= modifiedGraph;
				}
				return Task.FromResult(modified);
			}
		}

		private class PinNodeAndNeighboursOperation : Operation
		{
			private readonly NodeKey _targetNode;

			public PinNodeAndNeighboursOperation(NodeKey targetNode)
			{
				_targetNode = targetNode;
			}

			public override Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				var modified = false;

				var nodes = fullGraph.Nodes;
				var nodesToPin = nodes.ContainsKey(_targetNode) ?
					nodes[_targetNode].AllLinkKeys(includeThis: true) :
					ArrayUtils.GetEmpty<NodeKey>();

				foreach (var node in nodesToPin)
				{
					modified |= subgraph.AddPinnedNode(node, fullGraph);
				}

				return Task.FromResult(modified);
			}
		}

		private class AddDependencyOrDependentHierarchyOperation : SyncOperation
		{
			private readonly NodeKey _rootNodeKey;
			private readonly bool _isDependent;
			private readonly LinkType _dependencyRelationship;

			public AddDependencyOrDependentHierarchyOperation(NodeKey rootNodeKey, bool isDependent, LinkType dependencyRelationship)
			{
				_rootNodeKey = rootNodeKey;
				_isDependent = isDependent;
				_dependencyRelationship = dependencyRelationship;
			}
			protected override bool Apply(Subgraph subgraph, NodeGraph fullGraph)
			{
				if (fullGraph.Nodes.GetOrDefaultFromReadOnly(_rootNodeKey) is not { } rootNode)
				{
					return false;
				}

				var modified = false;
				var nodesSeen = new HashSet<Node>();
				var nodesToExplore = new Queue<Node>();

				void TryEnqueue(Node toEnqueue)
				{
					if (nodesSeen.Add(toEnqueue))
					{
						nodesToExplore.Enqueue(toEnqueue);
					}
				}

				TryEnqueue(rootNode);
				var lp = new LoopProtection();
				while (nodesToExplore.Count > 0)
				{
					lp.Iterate();
					var current = nodesToExplore.Dequeue();
					var links = _isDependent ? current.BackLinks : current.ForwardLinks;
					foreach (var dependency in links)
					{
						if (dependency.LinkType.HasFlagPartially(_dependencyRelationship))
						{
							TryEnqueue(_isDependent ? dependency.Dependent : dependency.Dependency);
						}
					}
				}

				foreach (var found in nodesSeen)
				{
					modified |= subgraph.AddPinnedNode(found.Key, fullGraph);
				}

				return modified;
			}
		}

		private class AddDirectDependenciesOrDependentsOperation : SyncOperation
		{
			private readonly NodeKey _rootNodeKey;
			private readonly bool _isDependent;
			private readonly LinkType _dependencyRelationship;

			public AddDirectDependenciesOrDependentsOperation(NodeKey rootNodeKey, bool isDependent, LinkType dependencyRelationship)
			{
				_rootNodeKey = rootNodeKey;
				_isDependent = isDependent;
				_dependencyRelationship = dependencyRelationship;
			}
			protected override bool Apply(Subgraph subgraph, NodeGraph fullGraph)
			{

				if (fullGraph.Nodes.GetOrDefaultFromReadOnly(_rootNodeKey) is not { } rootNode)
				{
					return false;
				}

				var modified = subgraph.AddPinnedNode(_rootNodeKey, fullGraph);

				var links = _isDependent ? rootNode.BackLinks : rootNode.ForwardLinks;
				foreach (var link in links)
				{
					if (link.LinkType.HasFlagPartially(_dependencyRelationship))
					{
						var node = _isDependent ? link.Dependent : link.Dependency;
						modified |= subgraph.AddPinnedNode(node.Key, fullGraph);
					}
				}
				return modified;
			}
		}

		private class AddAllInSameProjectOperation : SyncOperation
		{
			private readonly NodeKey _rootNodeKey;

			public AddAllInSameProjectOperation(NodeKey rootNodeKey)
			{
				this._rootNodeKey = rootNodeKey;
			}

			protected override bool Apply(Subgraph subgraph, NodeGraph fullGraph)
			{
				var rootNode = fullGraph.Nodes.GetOrDefaultFromReadOnly(_rootNodeKey);
				if (rootNode == null)
				{
					return false;
				}

				if ((rootNode as TypeNode)?.Project is not { } project)
				{
					return false;
				}

				var modified = false;
				foreach (var kvp in fullGraph.Nodes)
				{
					if (project.Equals((kvp.Value as TypeNode)?.Project))
					{
						modified |= subgraph.AddPinnedNode(kvp.Key, fullGraph);
					}
				}

				return modified;
			}
		}

		private class AddAllInSolutionOperation : SyncOperation
		{
			protected override bool Apply(Subgraph subgraph, NodeGraph fullGraph)
			{
				var modified = false;
				foreach (var kvp in fullGraph.Nodes)
				{
					modified |= subgraph.AddPinnedNode(kvp.Key, fullGraph);
				}
				return modified;
			}
		}

		private class SanitizeOperation : Operation
		{
			public override Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				var modified = false;

				foreach (var key in subgraph.AllNodes)
				{
					if (!fullGraph.Nodes.ContainsKey(key))
					{
						modified |= subgraph.RemoveNode(key);
					}
				}

				return Task.FromResult(modified);
			}
		}
	}
}
