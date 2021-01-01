#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Extensions;

namespace DependsOnThat.Graph
{
	public partial class Subgraph
	{
		/// <summary>
		/// An operation to add <paramref name="nodesToAdd"/> as <see cref="PinnedNodes"/> to a subgraph.
		/// </summary>
		public static Operation AddPinned(params NodeKey[] nodesToAdd) => new AddPinnedOperation(nodesToAdd);

		/// <summary>
		/// An operation to set a particular node as 'selected', adding it and its neighbours as <see cref="AdditionalNodes"/>.
		/// </summary>
		/// <param name="selected">Function to asynchronously resolve <see cref="NodeKey"/> for the 'selected' node.</param>
		/// <param name="maxLinks">The maximum number of links to be included. If there are more neighbours than this, they will be omitted.</param>
		public static Operation SetSelected(Func<CancellationToken, Task<NodeKey?>> selected, int maxLinks) => GetCompositeOperation(
			new ClearAdditionalsOperation(),
			new AddAdditionalFromRootOperation(selected, maxLinks)
		);

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

			public abstract Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct);

			public virtual Action? Callback => null;
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

		private class ClearAdditionalsOperation : Operation
		{
			public override Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct) => Task.FromResult(subgraph.ClearAdditional());
		}

		private class AddAdditionalFromRootOperation : Operation
		{
			private readonly Func<CancellationToken, Task<NodeKey?>> _getRoot;
			private readonly int _maxLinks;

			public AddAdditionalFromRootOperation(Func<CancellationToken, Task<NodeKey?>> getRoot, int maxLinks)
			{
				_getRoot = getRoot ?? throw new ArgumentNullException(nameof(getRoot));
				_maxLinks = maxLinks;
			}

			public override async Task<bool> Apply(Subgraph subgraph, NodeGraph fullGraph, CancellationToken ct)
			{
				var root = await _getRoot(ct);
				var nodes = fullGraph.Nodes;
				if (ct.IsCancellationRequested || root == null || !nodes.ContainsKey(root))
				{
					return false;
				}

				var modified = false;
				modified |= subgraph.AddAdditionalNode(root);

				var rootNode = nodes[root];
				if (rootNode.LinkCount <= _maxLinks)
				{
					foreach (var link in rootNode.AllLinks())
					{
						modified |= subgraph.AddAdditionalNode(link.Key);
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
