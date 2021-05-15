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

	}
}
