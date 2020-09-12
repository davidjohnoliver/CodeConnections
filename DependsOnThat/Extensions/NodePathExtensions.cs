#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Graph;
using DependsOnThat.Presentation;

namespace DependsOnThat.Extensions
{
	public static class NodePathExtensions
	{
		public static MultiDependencyDisplayEdge ToDisplayEdge(this NodePath path, int extensionDepth, IDictionary<Node, DisplayNode> displayNodes)
		{
			if (path.IntermediateLength <= extensionDepth * 2)
			{
				throw new ArgumentOutOfRangeException();
			}
			var source = path[extensionDepth];
			var target = path[^(extensionDepth + 1)];

			var range = path[extensionDepth..^extensionDepth];

			return new MultiDependencyDisplayEdge(displayNodes[source], displayNodes[target], GetPathInfo(range));
		}

		private static string GetPathInfo(Node[] range)
		{
			if (range.Length < 3)
			{
				throw new ArgumentOutOfRangeException("Multi-dependency edge should have at least 3 nodes");
			}
			var sb = new StringBuilder();
			sb.Append(range[0].ToDisplayString());
			for (int i = 1; i < range.Length; i++)
			{
				AppendArrow();
				sb.Append(range[i].ToDisplayString());
			}

			return sb.ToString();

			void AppendArrow()
			{
				sb.Append(" ->");
			}
		}
	}
}
