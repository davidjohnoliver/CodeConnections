#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeConnections.Extensions
{
	public static class ListExtensions
	{
		public static void AddRange<T>(this IList<T> list, IEnumerable<T> collection)
		{
			if (list is List<T> actualList)
			{
				actualList.AddRange(collection);
			}
			else
			{
				foreach (var item in collection)
				{
					list.Add(item);
				}
			}
		}

		/// <summary>
		/// Gets the difference of <paramref name="newCollection"/> from <paramref name="oldCollection"/>, ignoring ordering.
		/// </summary>
		/// <returns>
		///		IsDifferent: Are the item sets different?
		///		Added: Items in <paramref name="newCollection"/> but not <paramref name="oldCollection"/>.
		///		Removed: Items not in <paramref name="newCollection"/> that were present in <paramref name="oldCollection"/>.
		/// </returns>
		/// <remarks>This is intended to handle collections of unique items; duplicates will be ignored.</remarks>
		public static (bool IsDifferent, IList<T> Added, IList<T> Removed) GetUnorderedDiff<T>(this IEnumerable<T> newCollection, IEnumerable<T> oldCollection)
		{
			var oldSet = oldCollection.ToHashSet();
			var isDifferent = false;
			IList<T>? added = null;
			IList<T>? removed = null;

			foreach (var item in newCollection)
			{
				if (!oldSet.Remove(item))
				{
					added ??= new List<T>();
					isDifferent = true;

					added.Add(item);
				}
			}

			foreach (var remainingItem in oldSet)
			{
				removed ??= new List<T>();
				isDifferent = true;

				removed.Add(remainingItem);
			}

			return (isDifferent, added ?? ArrayUtils.GetEmpty<T>(), removed ?? ArrayUtils.GetEmpty<T>());
		}
	}
}
