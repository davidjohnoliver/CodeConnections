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
		public static DisplayNode ToDisplayNode(this Node node) => new DisplayNode(node switch
		{
			TypeNode typeNode => typeNode.Symbol.Name,
			_ => ""
		},
			node.IsRoot);
	}
}
