#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Graph.Display
{
	/// <summary>
	/// A node in the displayable subgraph.
	/// </summary>
	public class DisplayNode
	{
		public string DisplayString { get; }

		public string? FilePath { get; }

		public DisplayNode(string displayString, string? filePath)
		{
			DisplayString = displayString;
			FilePath = filePath;
		}

		public override bool Equals(object obj) => obj is DisplayNode otherNode && otherNode.DisplayString == DisplayString;

		public override int GetHashCode() => DisplayString?.GetHashCode() ?? 0;

		public override string ToString() => $"{nameof(DisplayNode)}-{DisplayString}";
	}
}
