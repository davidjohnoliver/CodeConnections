#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickGraph;

namespace DependsOnThat.Presentation
{
	/// <summary>
	/// An edge in the displayable subgraph.
	/// </summary>
	public class DisplayEdge : IEdge<DisplayNode>
	{
		public DisplayEdge(DisplayNode source, DisplayNode target)
		{
			Source = source;
			Target = target;
		}

		public DisplayNode Source { get; }

		public DisplayNode Target { get; }

		public string? Label { get; set; }

		public override bool Equals(object obj)
			=> obj is DisplayEdge otherEdge && Equals(Source, otherEdge.Source) && Equals(Target, otherEdge.Target) && Label == otherEdge.Label;

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 13;
				hash = hash * 31 + (Source?.GetHashCode() ?? 0);
				hash = hash * 31 + (Target?.GetHashCode() ?? 0);
				hash = hash * 31 + (Label?.GetHashCode() ?? 0);
				return hash;
			}
		}
	}
}
