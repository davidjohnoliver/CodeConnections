#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Git;
using CodeConnections.Graph;
using CodeConnections.Graph.Display;

namespace CodeConnections.Extensions
{
	public static class NodeExtensions
	{
		public static DisplayNode ToDisplayNode(this Node node, bool isPinned, object? parentContext) => new DisplayNode(
			ToDisplayString(node),
			node.Key,
			(node as TypeNode)?.FilePath,
			isPinned,
			node.GitStatus,
			(node as TypeNode)?.Project?.ProjectName,
			parentContext
		);

		public static string ToDisplayString(this Node node) => node switch
		{
			TypeNode typeNode => typeNode.Identifier.Name,
			_ => ""
		};

		public static IEnumerable<Node> AllLinks(this Node node) => node.ForwardLinkNodes.Concat(node.BackLinkNodes);

		/// <summary>
		/// Return keys for all links (back- and forward-) associated with <paramref name="node"/>.
		/// </summary>
		/// <param name="includeThis">Include key for <paramref name="node"/> itself?</param>
		public static IEnumerable<NodeKey> AllLinkKeys(this Node node, bool includeThis)
		{
			if (node is null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			if (includeThis)
			{
				yield return node.Key;
			}

			foreach (var link in node.ForwardLinks)
			{
				yield return link.Dependency.Key;
			}
			foreach (var link in node.BackLinks)
			{
				yield return link.Dependent.Key;
			}
		}
	}
}
