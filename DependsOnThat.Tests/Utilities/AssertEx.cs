#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Utilities;
using NUnit.Framework;

namespace DependsOnThat.Tests.Utilities
{
	public static class AssertEx
	{
		/// <summary>
		/// Asserts that <paramref name="enumerable"/> contains at least one element that satisfies <paramref name="expectedPredicate"/>.
		/// </summary>
		/// <returns>The first element found that satisfies the condition.</returns>
		public static TSource Contains<TSource>(IEnumerable<TSource> enumerable,Func<TSource, bool> expectedPredicate)
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
		/// 
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="enumerable"></param>
		/// <param name="expectedPredicate"></param>
		/// <returns></returns>
		public static TSource ContainsSingle<TSource>(IEnumerable<TSource> enumerable, Func<TSource, bool> expectedPredicate)
		{
			if (expectedPredicate is null)
			{
				throw new ArgumentNullException(nameof(expectedPredicate));
			}

			var lp = new LoopProtection();
			var hasFoundItem = false;
			TSource matchingItem = default;
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
	}
}
