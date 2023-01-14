#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace CodeConnections.Views.Behaviours
{
	public static class SelectorExtensions
	{

		public static ICommand GetItemClickCommand(Selector obj)
		{
			return (ICommand)obj.GetValue(ItemClickCommandProperty);
		}

		public static void SetItemClickCommand(Selector obj, ICommand value)
		{
			obj.SetValue(ItemClickCommandProperty, value);
		}

		// Using a DependencyProperty as the backing store for ItemClickCommand.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ItemClickCommandProperty =
			DependencyProperty.RegisterAttached("ItemClickCommand",
				typeof(ICommand),
				typeof(SelectorExtensions),
				 new PropertyMetadata(null, OnItemClickCommandChanged)
			);

		private static void OnItemClickCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var selector = (Selector)d;
			selector.SelectionChanged -= OnSelectionChanged;
			if (e.NewValue is ICommand)
			{
				selector.SelectionChanged += OnSelectionChanged;
			}

			static void OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
			{
				if (e.AddedItems.Count == 0)
				{
					return;
				}

				var item = e.AddedItems[0];
				if (sender is not Selector selectorInner)
				{
					return;
				}
				if (GetItemClickCommand(selectorInner) is not { } command)
				{
					return;
				}

				if (command.CanExecute(item))
				{
					command.Execute(item);
				}

				selectorInner.SelectedIndex = -1;
			}
		}

	}
}
