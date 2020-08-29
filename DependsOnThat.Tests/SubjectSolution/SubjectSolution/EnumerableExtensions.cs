using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubjectSolution
{
	public static class EnumerableExtensions
	{
		public static bool AnyOrNone<T>(this IEnumerable<T> source) => source.Any() || !source.Any();
	}
}
