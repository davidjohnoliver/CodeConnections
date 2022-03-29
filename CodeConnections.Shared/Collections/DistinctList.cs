#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CodeConnections.Collections
{
	/// <summary>
	/// A list that enforces distinctness. Attempts to add duplicate elements will do nothing.
	/// </summary>
	public class DistinctList<T> : IList<T>
	{
		private readonly List<T> _backing = new();
		private readonly ISet<T> _set;
		private readonly bool _isExternalDistinctnessSet;

		/// <summary>
		/// Create a new list.
		/// </summary>
		/// <param name="externalSet">
		/// An optional set to be used to check distinctness. This allows distinctness to be enforced at a level broader than this individual list.
		/// </param>
		public DistinctList(ISet<T>? externalSet = null)
		{
			_set = externalSet ?? new HashSet<T>();
			_isExternalDistinctnessSet = externalSet != null;
		}

		public T this[int index] { get => _backing[index]; set => _backing[index] = value; }

		public int Count => _backing.Count;

		public bool IsReadOnly => false;

		public void Add(T item)
		{
			if (_set.Add(item))
			{
				_backing.Add(item);
			}
		}

		public void Clear()
		{
			_backing.Clear();
			if (!_isExternalDistinctnessSet)
			{
				_set.Clear();
			}
		}

		public bool Contains(T item)
		{
			return _isExternalDistinctnessSet ? _backing.Contains(item) : _set.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			throw new NotSupportedException();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return ((IEnumerable<T>)_backing).GetEnumerator();
		}

		public int IndexOf(T item)
		{
			return _backing.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			if (_set.Add(item))
			{
				_backing.Insert(index, item);
			}
		}

		public bool Remove(T item)
		{
			if (_isExternalDistinctnessSet)
			{
				throw new InvalidOperationException("List is using an injected set to test distinctness, this operation is not supported.");
			}

			if (_set.Remove(item))
			{
				return _backing.Remove(item);
			}

			return false;
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_backing).GetEnumerator();
		}
	}
}
