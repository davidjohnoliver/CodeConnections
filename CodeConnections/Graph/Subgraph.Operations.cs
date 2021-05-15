﻿#nullable enable

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
		/// An operation to entirely remove <paramref name="nodesToRemove"/> from a subgraph.
		/// </summary>
		/// <param name="dontRemoveSelected">
		/// True: don't remove node if it's the selected node, just unpin it if pinned. False: remove selected node along with other nodes.
		/// </param>
		public static Operation Remove(bool dontRemoveSelected, params NodeKey[] nodesToRemove) => new RemoveNodeOperation(nodesToRemove, dontRemoveSelected);

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

		private class RemoveNodeOperation : Operation
		{
			private readonly IList<NodeKey> _nodeKeys;
			private readonly bool _dontRemoveSelected;

			public RemoveNodeOperation(IList<NodeKey> nodeKeys, bool dontRemoveSelected)
			{
				_nodeKeys = nodeKeys;
				_dontRemoveSelected = dontRemoveSelected;
			}

			public override Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				var modified = false;
				foreach (var node in _nodeKeys)
				{
					if (_dontRemoveSelected && Equals(subgraph._selectedNode, node))
					{
						modified |= subgraph.TogglePinned(node, setPinned: false);
					}
					else
					{
						modified |= subgraph.RemoveNode(node);
					}
				}

				return Task.FromResult(modified);
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
				var currentAdditionals = subgraph.AdditionalNodes;
				var nodes = fullGraph.Nodes;

				var newAdditionals = (nodes.ContainsKey(_selected), _includeConnectionsAsWell) switch
				{
					(false, _) => ArrayUtils.GetEmpty<NodeKey>(),
					(_, false) => new[] { _selected },
					(_, true) => nodes[_selected].AllLinkKeys(includeThis: true)
				};

				var (isDifferent, addedNodes, removedNodes) = newAdditionals.GetUnorderedDiff(currentAdditionals);

				var modified = false;
				if (isDifferent)
				{
					foreach (var removed in removedNodes)
					{
						modified |= subgraph.RemoveNode(removed);
					}
					foreach (var added in addedNodes)
					{
						modified |= subgraph.AddAdditionalNode(added, fullGraph);
					}
				}

				modified |= subgraph.SetSelectedNode(_selected);

				return Task.FromResult(modified);
			}
		}

		private class ClearSelectedOperation : Operation
		{
			public override Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				var modified = subgraph.ClearAdditional();
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
