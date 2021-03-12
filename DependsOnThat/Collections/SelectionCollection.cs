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
	/// Base class for a selection-aware collection.
	/// </summary>
	public abstract class SelectionCollection : ISelectionCollection
	{
		protected IList? SelectedItemsBacking { get; private set; }

		/// <summary>
		/// Raised whenever the currently selected items change.
		/// </summary>
		public event Action? SelectionChanged;

		void ISelectionCollection.RaiseSelectionChanged() => SelectionChanged?.Invoke();

		void ISelectionCollection.SetSelectedItemsBackingCollection(IList? backingCollection) => SelectedItemsBacking = backingCollection;

		protected bool SelectItem(object? item)
		{
			if (SelectedItemsBacking != null && !SelectedItemsBacking.Contains(item))
			{
				SelectedItemsBacking.Add(item);
				return true;
			}

			return false;
		}

		protected bool DeselectItem(object? item)
		{
			if (SelectedItemsBacking?.Contains(item) ?? false)
			{
				SelectedItemsBacking.Remove(item);
				return true;
			}

			return false;
		}
	}
}
