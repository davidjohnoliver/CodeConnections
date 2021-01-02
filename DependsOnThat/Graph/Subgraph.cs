#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Graph
{
	/// <summary>
	/// A subset of nodes from a <see cref="NodeGraph"/>.
	/// </summary>
	public sealed partial class Subgraph
	{
		public IEnumerable<NodeKey> AllNodes
		{
			get
			{
				foreach (var node in _pinnedNodes)
				{
					yield return node;
				}
				foreach (var node in _additionalNodes)
				{
					yield return node;
				}
			}
		}

		public int Count => _pinnedNodes.Count + _additionalNodes.Count;

		private readonly HashSet<NodeKey> _pinnedNodes = new HashSet<NodeKey>();
		/// <summary>
		/// Nodes that are considered 'pinned' to the subgraph.
		/// </summary>
		public IEnumerable<NodeKey> PinnedNodes => _pinnedNodes;

		private readonly HashSet<NodeKey> _additionalNodes = new HashSet<NodeKey>();
		/// <summary>
		/// Any other nodes included in the subgraph.
		/// </summary>
		public IEnumerable<NodeKey> AdditionalNodes => _additionalNodes;

		private bool AddPinnedNode(NodeKey nodeKey)
		{
			// Remove node from additional nodes, if it was there
			_additionalNodes.Remove(nodeKey);

			return _pinnedNodes.Add(nodeKey);
		}

		private bool AddAdditionalNode(NodeKey nodeKey)
		{
			if (_pinnedNodes.Contains(nodeKey))
			{
				// If node is already pinned, leave it pinned and don't do anything
				return false;
			}

			return _additionalNodes.Add(nodeKey);
		}

		private bool RemoveNode(NodeKey nodeKey) => _pinnedNodes.Remove(nodeKey) || _additionalNodes.Remove(nodeKey);

		public bool Clear()
		{
			if (_pinnedNodes.Count == 0 && _additionalNodes.Count == 0)
			{
				return false;
			}

			_pinnedNodes.Clear();
			_additionalNodes.Clear();

			return true;
		}

		private bool ClearAdditional()
		{
			if (_additionalNodes.Count == 0)
			{
				return false;
			}

			_additionalNodes.Clear();
			return true;
		}
	}
}
