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
		/// Total number of dependencies and dependents.
		/// </summary>
		public int LinkCount => ForwardLinks.Count + BackLinks.Count;

		/// <summary>
		/// Key with which the node can be retrieved from a collection.
		/// </summary>
		public abstract NodeKey Key { get; }

		/// <summary>
		/// A list of filepaths associated with this node.
		/// </summary>
		public IList<string> AssociatedFiles { get; } = new List<string>(1);

		/// <summary>
		/// Add <paramref name="forwardLink"/> as a new dependency of this node, setting this node as a dependent on <paramref name="forwardLink"/> 
		/// at the same time.
		/// </summary>
		/// <param name="forwardLink"></param>
		public void AddForwardLink(Node forwardLink)
		{
			_forwardLinks.Add(forwardLink);
			forwardLink._backLinks.Add(this);
		}

		/// <summary>
		/// Remove <paramref name="forwardLink"/> as a dependency of this node, updating the <see cref="BackLinks"/> collection on 
		/// <paramref name="forwardLink"/> at the same time.
		/// </summary>
		/// <param name="forwardLink"></param>
		public void RemoveForwardLink(Node forwardLink)
		{
			_forwardLinks.Remove(forwardLink);
			forwardLink._backLinks.Remove(this);
		}
	}
}
