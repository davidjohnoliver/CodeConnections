#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Collections
{
	/// <summary>
	/// A typed selection-aware list.
	/// </summary>
	/// <typeparam name="T">Item type.</typeparam>
	public class SelectionList<T> : SelectionCollection, IList<T>
	{
		private readonly IList<T> _inner;

		// TODO: maintain the selection independently of the bound backing view
		/// <summary>
		/// The items in the list which are currently selected.
		/// </summary>
		public IEnumerable<T> SelectedItems => SelectedItemsBacking?.Cast<T>() ?? Enumerable.Empty<T>();

		public SelectionList()
		{
			_inner = new List<T>();
		}

		public SelectionList(IEnumerable<T> collection)
		{
			_inner = new List<T>(collection);
		}

		public bool SelectItem(T item) => base.SelectItem(item);

		public bool DeselectItem(T item) => base.DeselectItem(item);

		#region IList<T>
		public T this[int index] { get => _inner[index]; set => _inner[index] = value; }

		public int Count => _inner.Count;

		public bool IsReadOnly => _inner.IsReadOnly;

		public void Add(T item) => _inner.Add(item);

		public void Clear() => _inner.Clear();

		public bool Contains(T item) => _inner.Contains(item);

		public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);

		public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();

		public int IndexOf(T item) => _inner.IndexOf(item);

		public void Insert(int index, T item) => _inner.Insert(index, item);

		public bool Remove(T item) => _inner.Remove(item);

		public void RemoveAt(int index) => _inner.RemoveAt(index);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		#endregion
	}
}
