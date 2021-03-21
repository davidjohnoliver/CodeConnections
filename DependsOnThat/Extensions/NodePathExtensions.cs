#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Graph;
using CodeConnections.Graph.Display;

namespace CodeConnections.Extensions
{
	public static class NodePathExtensions
	{
		public static MultiDependencyDisplayEdge ToDisplayEdge(this NodePath path, IDictionary<Node, DisplayNode> displayNodes)
		{
			if (path.IntermediateLength <= 0)
			{
				throw new ArgumentOutOfRangeException();
			}
			var source = path[0];
			var target = path[^(1)];

			var range = path[0..^0];

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
