#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickGraph;

namespace CodeConnections.Graph.Display
{
	/// <summary>
	/// An edge in the displayable subgraph.
	/// </summary>
	public abstract class DisplayEdge : IEdge<DisplayNode>
	{
		public DisplayEdge(DisplayNode source, DisplayNode target)
		{
			Source = source;
			Target = target;
		}

		public DisplayNode Source { get; }

		public DisplayNode Target { get; }

		public override bool Equals(object obj)
			=> obj is DisplayEdge otherEdge && Equals(Source, otherEdge.Source) && Equals(Target, otherEdge.Target);

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 13;
				hash = hash * 31 + (Source?.GetHashCode() ?? 0);
				hash = hash * 31 + (Target?.GetHashCode() ?? 0);
				return hash;
			}
		}

		public override string ToString() => $"{base.ToString()}-{Source} -> {Target}";
	}
}
