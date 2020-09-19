#nullable enable

using System.Collections;

namespace DependsOnThat.Collections
{
	/// <summary>
	/// View-facing methods for a selection-aware collection.
	/// </summary>
	public interface ISelectionCollection
	{
		void SetSelectedItemsBackingCollection(IList? backingCollection);

		void RaiseSelectionChanged();
	}
}