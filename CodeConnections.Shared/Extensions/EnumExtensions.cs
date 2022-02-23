using CodeConnections.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Extensions
{
	public static class EnumExtensions
	{
		public static bool HasAnyFlag<T>(this T enumValue, params T[] flags) where T : Enum
			=> flags.Any(f => enumValue.HasFlag(f));

		/// <summary>
		/// Is <paramref name="enumValue"/> a partial or complete match for <paramref name="flag"/>?
		/// </summary>
		/// <returns>True if the logical AND of <paramref name="enumValue"/> and <paramref name="flag"/> is non-zero, ie there is some overlap in their bits, false otherwise.</returns>
		public static bool HasFlagPartially<T>(this T enumValue, T flag) where T : Enum
		{
			return ((int)(object)enumValue & (int)(object)flag) != 0;
		}

		/// <summary>
		/// Decomposes <paramref name="enumValue"/> into all single-bit values that belong to <typeparamref name="T"/>.
		/// E.g. if enumValue = 2 | 4, this will return (T)2 and (T)4, as long as they are both defined among T's possible values.
		/// </summary>
		/// <typeparam name="T">This would typically be a Flags-type enum</typeparam>
		public static IEnumerable<T> Decompose<T>(this T enumValue) where T : Enum
		{
			foreach (var fieldValue in EnumUtils.GetSingleFieldValues<T>())
			{
				if (enumValue.Equals(fieldValue))
				{
					yield return fieldValue;
					// If value is an exact match for a particular single-bit field, it will not match any other fields
					yield break;
				}
				if (enumValue.HasFlagPartially(fieldValue))
				{
					yield return fieldValue;
				}
			}
		}
	}
}
