#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft;

namespace DependsOnThat.Extensions
{
	public static class DictionaryExtensions
	{
		[return: MaybeNull]
		public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			if (dictionary.TryGetValue(key, out var value))
			{
				return value;
			}

			return default;
		}
	}
}
