#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeConnections.Collections
{
	/// <summary>
	/// A dictionary of lists. A list will be automatically generated for a given key if it doesn't exist.
	/// </summary>
	public class CollectionDictionary<TKey, TValue> : IDictionary<TKey, IList<TValue>>
	{
		private readonly Dictionary<TKey, IList<TValue>> _backing = new();
		private readonly HashSet<TValue>? _globalSet;

		public int ItemsCount => Values.Sum(l => l.Count);

		/// <summary>
		/// Create a new collection dictionary.
		/// </summary>
		/// <param name="enforceGlobalDistinctness">
		/// If true, will try to enforce that at most one copy of an item is present in any of the keyed collections. By default false.
		/// </param>
		public CollectionDictionary(bool enforceGlobalDistinctness = false)
		{
			if (enforceGlobalDistinctness)
			{
				_globalSet = new();
			}
		}

		public IList<TValue> this[TKey key]
		{
			get
			{
				EnsurePresent(key);
				return ((IDictionary<TKey, IList<TValue>>)_backing)[key];
			}
			set => ((IDictionary<TKey, IList<TValue>>)_backing)[key] = value;
		}

		private void EnsurePresent(TKey key)
		{
			if (!ContainsKey(key))
			{
				_backing[key] = _globalSet != null ? new DistinctList<TValue>(_globalSet) : new List<TValue>();
			}
		}

		public ICollection<TKey> Keys => ((IDictionary<TKey, IList<TValue>>)_backing).Keys;

		public ICollection<IList<TValue>> Values => ((IDictionary<TKey, IList<TValue>>)_backing).Values;

		public int Count => ((ICollection<KeyValuePair<TKey, IList<TValue>>>)_backing).Count;

		public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, IList<TValue>>>)_backing).IsReadOnly;

		public void Add(TKey key, IList<TValue> value)
		{
			((IDictionary<TKey, IList<TValue>>)_backing).Add(key, value);
		}

		public void Add(KeyValuePair<TKey, IList<TValue>> item)
		{
			((ICollection<KeyValuePair<TKey, IList<TValue>>>)_backing).Add(item);
		}

		public void Clear()
		{
			((ICollection<KeyValuePair<TKey, IList<TValue>>>)_backing).Clear();
			_globalSet?.Clear();
		}

		public bool Contains(KeyValuePair<TKey, IList<TValue>> item)
		{
			return ((ICollection<KeyValuePair<TKey, IList<TValue>>>)_backing).Contains(item);
		}

		public bool ContainsKey(TKey key)
		{
			return ((IDictionary<TKey, IList<TValue>>)_backing).ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<TKey, IList<TValue>>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<TKey, IList<TValue>>>)_backing).CopyTo(array, arrayIndex);
		}

		public IEnumerator<KeyValuePair<TKey, IList<TValue>>> GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<TKey, IList<TValue>>>)_backing).GetEnumerator();
		}

		public bool Remove(TKey key)
		{
			return ((IDictionary<TKey, IList<TValue>>)_backing).Remove(key);
		}

		public bool Remove(KeyValuePair<TKey, IList<TValue>> item)
		{
			return ((ICollection<KeyValuePair<TKey, IList<TValue>>>)_backing).Remove(item);
		}

		public bool TryGetValue(TKey key, out IList<TValue> value)
		{
			return ((IDictionary<TKey, IList<TValue>>)_backing).TryGetValue(key, out value);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_backing).GetEnumerator();
		}
	}
}
