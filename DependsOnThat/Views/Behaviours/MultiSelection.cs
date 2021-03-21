#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CodeConnections.Collections;

namespace CodeConnections.Views.Behaviours
{
	/// <summary>
	/// Supports setting a selection-aware collection (<see cref="SelectionCollection"/>) as the ItemsSource of a <see cref="ListBox"/>.
	/// </summary>
	public static class MultiSelection
	{
		public static ISelectionCollection GetItemsSource(ListBox obj) => (ISelectionCollection)obj.GetValue(ItemsSourceProperty);

		public static void SetItemsSource(ListBox obj, ISelectionCollection value) => obj.SetValue(ItemsSourceProperty, value);

		/// <summary>
		/// This will apply the ItemsSource to the list it's attached to, and if it's a <see cref="SelectionCollection"/> configure it to back-propagate selected items and selection changes.
		/// </summary>
		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.RegisterAttached("ItemsSource", typeof(ISelectionCollection), typeof(MultiSelection), new PropertyMetadata(null, (o, e) => OnItemsSourceChanged((ListBox)o, e.OldValue, e.NewValue)));

		private static void OnItemsSourceChanged(ListBox listBox, object oldValue, object newValue)
		{
			listBox.SelectionChanged -= ListBox_SelectionChanged;
			if (oldValue is ISelectionCollection oldCollection)
			{
				oldCollection.SetSelectedItemsBackingCollection(null);
			}

			if (newValue is ISelectionCollection newCollection)
			{
				listBox.SelectionMode = SelectionMode.Multiple;
				listBox.SelectionChanged += ListBox_SelectionChanged;
				newCollection.SetSelectedItemsBackingCollection(listBox.SelectedItems);
			}

			listBox.ItemsSource = newValue as IEnumerable;

			UpdateSelectionStates(listBox);
		}

		private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender is ListBox listBox && listBox.ItemsSource is ISelectionCollection selectionCollection)
			{
				selectionCollection.RaiseSelectionChanged();
			}
		}


		public static SelectionState GetInitialSelectionState(ListBox obj) => (SelectionState)obj.GetValue(InitialSelectionStateProperty);

		public static void SetInitialSelectionState(ListBox obj, SelectionState value) => obj.SetValue(InitialSelectionStateProperty, value);

		/// <summary>
		/// Sets the initial selection state for items that have just been added to the list.
		/// </summary>
		public static readonly DependencyProperty InitialSelectionStateProperty =
			DependencyProperty.RegisterAttached("InitialSelectionState", typeof(SelectionState), typeof(MultiSelection), new PropertyMetadata(SelectionState.Unselected, (o, e) => UpdateSelectionStates((ListBox)o)));

		/// <summary>
		/// Apply the initial selection state for all items.
		/// </summary>
		private static void UpdateSelectionStates(ListBox listBox)
		{
			var selectionState = GetInitialSelectionState(listBox);
			if (selectionState == SelectionState.Selected && listBox.ItemsSource != null)
			{
				foreach (var item in listBox.ItemsSource)
				{
					listBox.SelectedItems.Add(item);
				}
			}
		}

		public enum SelectionState
		{
			Selected,
			Unselected
		}
	}
}
