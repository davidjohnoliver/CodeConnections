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

		[return: MaybeNull]
		public static TValue GetOrDefaultFromReadOnly<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
		{
			if (dictionary.TryGetValue(key, out var value))
			{
				return value;
			}

			return default;
		}

		public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> initializer)
		{
			if (initializer is null)
			{
				throw new ArgumentNullException(nameof(initializer));
			}

			if (!dictionary.ContainsKey(key))
			{
				dictionary[key] = initializer(key);
			}

			return dictionary[key];
		}
	}
}
