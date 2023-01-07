#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Utilities;
using NUnit.Framework;

namespace CodeConnections.Tests.Utilities
{
	public static class AssertEx
	{
		public static void Contains<TSource>(IEnumerable<TSource> enumerable, TSource expectedItem)
		{
			var lp = new LoopProtection();
			foreach (var item in enumerable)
			{
				lp.Iterate();
				if (Equals(item, expectedItem))
				{
					return;
				}
			}

			throw new AssertionException($"{enumerable} does not contain {expectedItem}");
		}
		public static void DoesNotContain<TSource>(IEnumerable<TSource> enumerable, TSource expectedItem)
		{
			var lp = new LoopProtection();
			foreach (var item in enumerable)
			{
				lp.Iterate();
				if (Equals(item, expectedItem))
				{
					throw new AssertionException($"{enumerable} does contain {expectedItem}");
				}
			}

		}

		/// <summary>
		/// Asserts that <paramref name="enumerable"/> contains at least one element that satisfies <paramref name="expectedPredicate"/>.
		/// </summary>
		/// <returns>The first element found that satisfies the condition.</returns>
		public static TSource Contains<TSource>(IEnumerable<TSource> enumerable, Func<TSource, bool> expectedPredicate)
		{
			if (expectedPredicate is null)
			{
				throw new ArgumentNullException(nameof(expectedPredicate));
			}

			var lp = new LoopProtection();
			foreach (var item in enumerable)
			{
				lp.Iterate();
				if (expectedPredicate(item))
				{
					return item;
				}
			}

			throw new AssertionException($"{enumerable} contains no items satisfying the supplied condition");
		}

		/// <summary>
		/// Asserts that <paramref name="enumerable"/> contains exactly one element that satisfies <paramref name="expectedPredicate"/>.
		/// </summary>
		/// <returns>The element that satisfies the condition.</returns>
		public static TSource ContainsSingle<TSource>(IEnumerable<TSource> enumerable, Func<TSource, bool> expectedPredicate)
		{
			if (expectedPredicate is null)
			{
				throw new ArgumentNullException(nameof(expectedPredicate));
			}

			var lp = new LoopProtection();
			var hasFoundItem = false;
			TSource? matchingItem = default;
			foreach (var item in enumerable)
			{
				lp.Iterate();
				if (expectedPredicate(item))
				{
					if (hasFoundItem)
					{
						throw new AssertionException($"{enumerable} contains more than one element");
					}
					else
					{
						hasFoundItem = true;
						matchingItem = item;
					}
				}
			}

			if (hasFoundItem)
			{
				return matchingItem!;
			}

			throw new AssertionException($"{enumerable} contains no items satisfying the supplied condition");
		}

		/// <summary>
		/// Asserts that none of the elements in <paramref name="enumerable"/> match <paramref name="excludedPredicate"/>.
		/// </summary>
		public static void None<TSource>(IEnumerable<TSource> enumerable, Func<TSource, bool> excludedPredicate)
		{
			if (excludedPredicate is null)
			{
				throw new ArgumentNullException(nameof(excludedPredicate));
			}

			var lp = new LoopProtection();
			foreach (var item in enumerable)
			{
				lp.Iterate();
				if (excludedPredicate(item))
				{
					throw new AssertionException($"{item} matching excluded condition found in {enumerable}");
				}
			}
		}
	}
}
