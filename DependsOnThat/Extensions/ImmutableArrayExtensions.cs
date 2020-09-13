using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Extensions
{
	public static class ImmutableArrayExtensions
	{
		/// <summary>
		/// Returns the <see cref="ImmutableArray{T}.Length"/> of an <see cref="ImmutableArray{T}"/> if it's initialized, or 0 if it's not.
		/// </summary>
		public static int GetLengthSafe<T>(this ImmutableArray<T> array) => array.IsDefault ? 0 : array.Length;
	}
}
