﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Presentation
{
	/// <summary>
	/// A node in the displayable subgraph.
	/// </summary>
	public class DisplayNode
	{
		public string DisplayString { get; }

		public DisplayNode(string displayString)
		{
			DisplayString = displayString;
		}

		public override bool Equals(object obj) => obj is DisplayNode otherNode && otherNode.DisplayString == DisplayString;

		public override int GetHashCode() => DisplayString?.GetHashCode() ?? 0;
	}
}
