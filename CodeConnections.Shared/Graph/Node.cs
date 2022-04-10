#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Git;

namespace CodeConnections.Graph
{
	/// <summary>
	/// A node in the dependency graph.
	/// </summary>
	public abstract partial class Node
	{
		private readonly HashSet<Link> _forwardLinks = new();
		private readonly HashSet<Link> _backLinks = new();

		/// <summary>
		/// The nodes that this node depends upon.
		/// </summary>
		public IReadOnlyCollection<Link> ForwardLinks => _forwardLinks;

		public IEnumerable<Node> ForwardLinkNodes => _forwardLinks.Select(l => l.Dependency);

		/// <summary>
		/// The nodes that depend upon this node.
		/// </summary>
		public IReadOnlyCollection<Link> BackLinks => _backLinks;

		public IEnumerable<Node> BackLinkNodes => _backLinks.Select(l => l.Dependent);

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

		public GitStatus GitStatus { get; set; }

		private SubgraphState? _subgraphState;
		/// <summary>
		/// Values set by subgraph operations. Values within are not guaranteed to be fresh or set at all except in a display context, and
		/// then only when the corresponding subgraph inclusion condition can be inferred to be active.
		/// </summary>
		public SubgraphState SubgraphValues => _subgraphState ??= new();

		/// <summary>
		/// Add <paramref name="forwardLink"/> as a new dependency of this node, setting this node as a dependent on <paramref name="forwardLink"/> 
		/// at the same time.
		/// </summary>
		/// <param name="forwardLink"></param>
		public void AddForwardLink(Node forwardLink, LinkType linkType)
		{
			var link = new Link(forwardLink, this, linkType);
			_forwardLinks.Add(link);
			forwardLink._backLinks.Add(link);
		}

		/// <summary>
		/// Remove <paramref name="forwardLink"/> as a dependency of this node, updating the <see cref="BackLinks"/> collection on 
		/// <paramref name="forwardLink"/> at the same time.
		/// </summary>
		public void RemoveForwardLink(Link link)
		{
				_forwardLinks.Remove(link);
				link.Dependency._backLinks.Remove(link);
		}


		/// <summary>
		/// Remove <paramref name="forwardLink"/> as a dependency of this node, updating the <see cref="BackLinks"/> collection on 
		/// <paramref name="forwardLink"/> at the same time.
		/// </summary>
		public void RemoveForwardLink(Node forwardLink)
		{
			var link = _forwardLinks.FirstOrDefault(l => l.Dependency == forwardLink);
			if (link != null)
			{
				_forwardLinks.Remove(link);
				forwardLink._backLinks.Remove(link);
			}
		}
	}
}
