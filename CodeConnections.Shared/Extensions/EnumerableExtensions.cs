#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Extensions
{
	public static class EnumerableExtensions
	{
		/// <summary>
		/// Returns an enumerable with any null values removed.
		/// </summary>
		public static IEnumerable<T> Trim<T>(this IEnumerable<T?> enumerable) where T : class
		{
			foreach (var t in enumerable)
			{
				if (t != null)
				{
					yield return t;
				}
			}
		}

		public static bool None<T>(this IEnumerable<T> enumerable) => !enumerable.Any();

		public static IEnumerable<T> Except<T>(this IEnumerable<T> enumerable, T excludedValue)
		{
			foreach (var t in enumerable)
			{
				if (!Equals(t, excludedValue))
				{
					yield return t;
				}
			}
		}
	}
}
