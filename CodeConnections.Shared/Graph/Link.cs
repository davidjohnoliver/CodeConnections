﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Graph
{
	/// <summary>
	/// Describes a dependency relationship.
	/// </summary>
	public sealed record Link(Node Dependency, Node Dependent, LinkType LinkType)
	{
		public Node ForDirection(Direction direction) => direction switch
		{
			Direction.Dependent => Dependent,
			Direction.Dependency => Dependency,
			_ => throw new ArgumentException()
		};
	}
}
