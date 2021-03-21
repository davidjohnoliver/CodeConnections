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
	}
}
