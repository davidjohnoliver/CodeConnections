#nullable enable

using CodeConnections.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeConnections.Extensions
{
	public static class ImportanceExtensions
	{
		public static Subgraph.InclusionCategory ToInclusionCategory(this Importance importance) => importance switch
		{
			Importance.Low => Subgraph.InclusionCategory.ImportanceLow,
			Importance.Intermediate => Subgraph.InclusionCategory.ImportanceIntermediate,
			Importance.High => Subgraph.InclusionCategory.ImportanceHigh,
			_ => throw new ArgumentException()
		};
	}
}
