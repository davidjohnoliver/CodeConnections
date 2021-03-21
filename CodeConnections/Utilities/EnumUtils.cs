#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Utilities
{
	public static class EnumUtils
	{
		/// <summary>
		/// Get all constant values of enum <typeparamref name="T"/>.
		/// </summary>
		public static T[] GetValues<T>() where T : Enum => Enum.GetValues(typeof(T)).Cast<T>().ToArray(); // TODO: should be cached
	}
}
