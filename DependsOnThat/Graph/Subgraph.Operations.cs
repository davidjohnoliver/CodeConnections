#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using DependsOnThat.Utilities;

namespace DependsOnThat.Graph
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

		/// <param name="maxLinks">The maximum number of links to be included. If there are more neighbours than this, they will be omitted.</param>
		public static Operation SetSelected(NodeKey selected, int maxLinks) => new UpdateSelectedOperation(selected, maxLinks);

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
			protected Operation()
			{

			}

			/// <summary>
			/// Applies this operation to <paramref name="subgraph"/>.
			/// </summary>
			/// <param name="fullGraph">The full <see cref="NodeGraph"/> set.</param>
			/// <returns>True if <paramref name="subgraph"/> was modified by the operation, false otherwise.</returns>

			public abstract Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct); // TODO: there's no longer a compelling reason for this to be asynchronous
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
						modified |= subgraph.AddPinnedNode(node);
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

			private readonly int _maxLinks;

			public UpdateSelectedOperation(NodeKey selected, int maxLinks)
			{
				_selected = selected ?? throw new ArgumentNullException(nameof(selected));
				_maxLinks = maxLinks;
			}

			public override Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				var currentAdditionals = subgraph.AdditionalNodes;
				var nodes = fullGraph.Nodes;
				var newAdditionals = nodes.ContainsKey(_selected) ?
					nodes[_selected].AllLinkKeys(includeThis: true) :
					ArrayUtils.GetEmpty<NodeKey>();

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
						modified |= subgraph.AddAdditionalNode(added);
					}
				}

				modified |= subgraph.SetSelected(_selected);

				return Task.FromResult(modified);
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
