#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Extensions;

namespace DependsOnThat.Utilities
{
	public static class ArrayUtils
	{
		private static readonly Dictionary<Type, Array> _cachedEmptyArrays = new Dictionary<Type, Array>();
		/// <summary>
		///  Creates a new array with pre-initialized values.
		/// </summary>
		/// <typeparam name="T">Array type</typeparam>
		/// <param name="length">Array length</param>
		/// <param name="initializer">Initializer function for values, which takes array index and returns initialized value for that position.</param>
		public static T[] Create<T>(int length, Func<int, T> initializer)
		{
			if (initializer is null)
			{
				throw new ArgumentNullException(nameof(initializer));
			}

			var array = new T[length];
			for (int i = 0; i < length; i++)
			{
				array[i] = initializer(i);
			}
			return array;
		}

		/// <summary>
		/// Returns an empty array of <typeparamref name="T"/>.
		/// </summary>
		public static T[] GetEmpty<T>()
		{
			lock (_cachedEmptyArrays)
			{
				var array = _cachedEmptyArrays.GetOrCreate(typeof(T), _ => new T[0]);
				return (T[])array;
			}
		}
	}
}
