#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Utilities
{
	public static class ArrayUtils
	{
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
	}
}
