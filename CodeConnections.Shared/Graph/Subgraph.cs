#nullable enable

using CodeConnections.Extensions;
using CodeConnections.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Graph
{
	/// <summary>
	/// A subset of nodes from a <see cref="NodeGraph"/>.
	/// </summary>
	/// <remarks>
	/// The subgraph only stores the node keys. A given subgraph instance is implicitly associated with a
	/// particular <see cref="NodeGraph"/>, which may be required to be passed to the subgraph to make 
	/// modifications.
	/// </remarks>
	public sealed partial class Subgraph
	{
		private readonly Dictionary<NodeKey, InclusionCategory> _nodes = new();

		private readonly Dictionary<InclusionCategory, HashSet<NodeKey>> _nodesByCategory = new();

		public IEnumerable<NodeKey> AllNodes
		{
			get
			{
				return _nodes.Keys;
			}
		}
		public int Count => _nodes.Count;

		/// <summary>
		/// Injected function which allows filtering rules to be applied to reject nodes from subgraph.
		/// </summary>
		private readonly Func<NodeKey, NodeGraph, bool> _shouldIncludeNode;

		public Subgraph(Func<NodeKey, NodeGraph, bool> shouldIncludeNode)
		{
			_shouldIncludeNode = shouldIncludeNode;
		}

		/// <summary>
		/// Get all nodes belonging to a particular category.
		/// </summary>
		/// <param name="simpleCategory">A category that is expected to be a single-bit value of <see cref="InclusionCategory"/>.</param>
		private IReadOnlyCollection<NodeKey> GetNodesForCategory(InclusionCategory simpleCategory)
		{
			if (_nodesByCategory.TryGetValue(simpleCategory, out var nodesForCategory))
			{
				return nodesForCategory;
			}
			else
			{
				return ArrayUtils.GetEmpty<NodeKey>();
			}
		}

		private bool IsInCategory(NodeKey node, InclusionCategory category)
		{
			if (_nodes.TryGetValue(node, out var categories))
			{
				return (categories & category) == category;
			}

			return false;
		}

		/// <summary>
		/// Add node to the subgraph under a particular category. If <paramref name="node"/> already belongs to the subgraph,
		/// <paramref name="category"/> will be applied if it's not already.
		/// </summary>
		/// <returns>
		/// A tuple whose first value will be true if the node was not in the subgraph at all and was added, and
		/// whose second value will be true if the node was not in the category and was added to it.
		/// </returns>
		private (bool AddedToSubgraph, bool AddedToCategory) AddNodeUnderCategory(NodeKey node, InclusionCategory category, NodeGraph nodeGraph)
		{
			if (!_shouldIncludeNode(node, nodeGraph))
			{
				return (false, false);
			}

			return AddNodeUnderCategory(node, category);
		}


		/// <summary>
		/// Add node to the subgraph under a particular category. If <paramref name="node"/> already belongs to the subgraph,
		/// <paramref name="category"/> will be applied if it's not already.
		/// </summary>
		/// <remarks>
		/// This overload should only be called for nodes that are known to *already* be included in the subgraph, because it skips the check as to
		/// whether they should be included at all.
		/// </remarks>
		private (bool AddedToSubgraph, bool AddedToCategory) AddNodeUnderCategory(NodeKey node, InclusionCategory category)
		{
			foreach (var categorySimple in category.Decompose())
			{
				var categorySet = _nodesByCategory.GetOrCreate(categorySimple, _ => new());
				categorySet.Add(node);
			}

			if (_nodes.TryGetValue(node, out var currentCategories))
			{
				if ((currentCategories & category) != category)
				{
					_nodes[node] = currentCategories | category;
					// Added to new category, already in subgraph
					return (false, true);
				}
				else
				{
					// Already in subgraph in nominated category
					return (false, false);
				}
			}
			else
			{
				_nodes[node] = category;
				// New to subgraph
				return (true, true);
			}
		}

		/// <summary>
		/// Unapply <paramref name="category"/> from <paramref name="node"/>. If <paramref name="node"/> no longer falls under any categories,
		/// it will be removed from the subgraph entirely.
		/// </summary>
		/// <returns>
		/// A tuple whose first value was true if the node was removed from its only category and therefore from the subgraph
		/// entirely, and whose second value was true if it did indeed belong to the category and was thus removed from it.
		/// </returns>
		private (bool RemovedFromSubgraph, bool RemovedFromCategory) RemoveNodeFromCategory(NodeKey node, InclusionCategory category)
		{
			if (_nodes.TryGetValue(node, out var categories) /*&& (categories & category) != InclusionCategory.None*/)
			{
				foreach (var categorySimple in category.Decompose())
				{
					if (_nodesByCategory.TryGetValue(categorySimple, out var categorySet))
					{
						categorySet.Remove(node);
					}
				}
				var newCategories = categories & ~category;
				switch (newCategories)
				{
					case InclusionCategory.None:
						_nodes.Remove(node);
						// No remaining categories, removed completely
						return (true, true);
					case { } unchanged when unchanged == categories:
						// Was not in category
						return (false, false);
					default:
						_nodes[node] = newCategories;
						// Removed from category but still included under other categories
						return (false, true);

				}
			}

			// Was not in subgraph at all
			return (false, false);
		}

		/// <summary>
		/// Remove <<paramref name="node"/> from <paramref name="categoryToRemove"/>, but, if it's not pinned,
		/// (and it was indeed in <paramref name="categoryToRemove"/>,) mark it as <see cref="InclusionCategory.Unpinned"/>,
		/// keeping it in the graph for a little while longer.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="categoryToRemove"></param>
		/// <returns></returns>
		private bool RemoveNodeFromCategoryAndLeaveUnpinned(NodeKey node, InclusionCategory categoryToRemove/*, NodeGraph fullGraph*/)
		{
			if (_nodes.TryGetValue(node, out var currentCategories))
			{
				if ((currentCategories & categoryToRemove) == InclusionCategory.None)
				{
					// Not actually currently in category
					return false;
				}

				if ((currentCategories & InclusionCategory.Pinned) == InclusionCategory.None || categoryToRemove == InclusionCategory.Pinned)
				{
					// It's not pinned, or pinned is what we're removing it from, so mark it unpinned
					AddNodeUnderCategory(node, InclusionCategory.Unpinned);
				}

				RemoveFromCategory(node, categoryToRemove);

				return true;
			}

			// Was not in subgraph at all
			return false;
		}

		/// <summary>
		/// Add <paramref name="nodeKey"/> as a pinned node. If it's currently present and not pinned, pin it.
		/// </summary>
		/// <returns>True if the node was added to pinned nodes (and was not already pinned), false otherwise.</returns>
		private bool AddPinnedNode(NodeKey nodeKey, NodeGraph nodeGraph) 
			=> AddNodeUnderCategory(nodeKey, InclusionCategory.Pinned, nodeGraph).AddedToCategory;

		/// <summary>
		/// Removes <paramref name="node"/> from all categories, removing it from the subgraph.
		/// </summary>
		/// <returns>True if the node was removed from the subgraph, false if it was not present.</returns>
		private bool RemoveNode(NodeKey node)
		{
			if (_nodes.TryGetValue(node, out var category))
			{
				foreach (var categorySimple in category.Decompose())
				{
					if (_nodesByCategory.TryGetValue(categorySimple, out var categorySet))
					{
						categorySet.Remove(node);
					}
				}
			}
			return _nodes.Remove(node);
		}

		/// <summary>
		/// Moves <paramref name="node"/> category from <see cref="InclusionCategory.Pinned"/> to <see cref="InclusionCategory.Unpinned"/> or vice versa, depending 
		/// on <paramref name="setPinned"/>. Will have no effect if node is not found in the source, nor if trying to set to unpinned a node that is currently not pinned.
		/// </summary>
		public bool TogglePinned(NodeKey node, bool setPinned)
		{
			if (_nodes.TryGetValue(node, out var currentCategories))
			{
				if (setPinned)
				{
					var tryToggle = AddNodeUnderCategory(node, InclusionCategory.Pinned);
					RemoveFromCategory(node, InclusionCategory.Unpinned);
					return tryToggle.AddedToCategory;
				}
				else if ((currentCategories & InclusionCategory.Pinned) != InclusionCategory.None)
				{
					RemoveNodeFromCategoryAndLeaveUnpinned(node, InclusionCategory.Pinned);
					return true;
				}
			}

			return false;
		}

		public bool IsPinned(NodeKey node) => IsInCategory(node, InclusionCategory.Pinned);

		/// <summary>
		/// Describes the reason(s) for a node to be included in the subgraph.
		/// </summary>
		[Flags]
		public enum InclusionCategory
		{
			/// <summary>
			/// The node has no reason to be in the subgraph (and should not be).
			/// </summary>
			None = 0,
			/// <summary>
			/// The node is pinned by an explicit user interaction.
			/// </summary>
			Pinned = 1 << 0,
			/// <summary>
			/// The node is not pinned to the graph. If it has no other category than this, it's likely to be removed in the future
			/// following certain user interactions, but for now it's visible and the user may eg pin it to the graph if they wish.
			/// </summary>
			Unpinned = 1 << 1,
			/// <summary>
			/// The node is currently selected.
			/// </summary>
			Selected = 1 << 2,
			/// <summary>
			/// The node is a neighbour of the currently selected node.
			/// </summary>
			NeighbourOfSelected = 1 << 3,
			/// <summary>
			/// The node has changes in source control, and Git mode is enabled.
			/// </summary>
			GitChanges = 1 << 4,
		}
	}
}
