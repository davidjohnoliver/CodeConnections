using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Graph;
using DependsOnThat.Presentation;

namespace DependsOnThat.Extensions
{
	public static class NodeExtensions
	{
		public static DisplayNode ToDisplayNode(this Node node, bool isRoot) => new DisplayNode(
			ToDisplayString(node),
			isRoot,
			(node as TypeNode)?.FilePath
		);

		public static string ToDisplayString(this Node node) => node switch
		{
			TypeNode typeNode => typeNode.Identifier.Name,
			_ => ""
		};

		public static IEnumerable<Node> AllLinks(this Node node) => node.ForwardLinks.Concat(node.BackLinks);
	}
}
