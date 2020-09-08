#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Graph
{
	/// <summary>
	/// A node in the dependency graph.
	/// </summary>
	public abstract class Node
	{
		private readonly HashSet<Node> _forwardLinks = new HashSet<Node>();
		private readonly HashSet<Node> _backLinks = new HashSet<Node>();

		/// <summary>
		/// The nodes that this node depends upon.
		/// </summary>
		public IReadOnlyCollection<Node> ForwardLinks => _forwardLinks;

		/// <summary>
		/// The nodes that depend upon this node.
		/// </summary>
		public IReadOnlyCollection<Node> BackLinks => _backLinks;

		/// <summary>
		/// Key with which the node can be retrieved from a collection.
		/// </summary>
		public abstract NodeKey Key { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="forwardLink"></param>
		public void AddForwardLink(Node forwardLink)
		{
			_forwardLinks.Add(forwardLink);
			forwardLink._backLinks.Add(this);
		}
	}
}
