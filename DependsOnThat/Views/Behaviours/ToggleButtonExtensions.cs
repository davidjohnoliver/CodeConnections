#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using DependsOnThat.Input;

namespace DependsOnThat.Views.Behaviours
{
	public static class ToggleButtonExtensions
	{


		public static object? GetToggleCommandParameter(ToggleButton obj)
		{
			return (object)obj.GetValue(ToggleCommandParameterProperty);
		}

		public static void SetToggleCommandParameter(ToggleButton obj, object? value)
		{
			obj.SetValue(ToggleCommandParameterProperty, value);
		}

		public static readonly DependencyProperty ToggleCommandParameterProperty =
			DependencyProperty.RegisterAttached("ToggleCommandParameter", typeof(object), typeof(ToggleButtonExtensions), new PropertyMetadata(defaultValue: null));

		public static IToggleCommand? GetToggleCommand(ToggleButton obj)
		{
			return (IToggleCommand)obj.GetValue(ToggleCommandProperty);
		}

		public static void SetToggleCommand(ToggleButton obj, IToggleCommand? value)
		{
			obj.SetValue(ToggleCommandProperty, value);
		}

		public static readonly DependencyProperty ToggleCommandProperty =
			DependencyProperty.RegisterAttached("ToggleCommand", typeof(IToggleCommand), typeof(ToggleButtonExtensions), new PropertyMetadata((o, e) => OnToggleCommandChanged((ToggleButton)o, (IToggleCommand?)e.OldValue, (IToggleCommand?)e.NewValue)));

		private static void OnToggleCommandChanged(ToggleButton toggleButton, IToggleCommand? oldValue, IToggleCommand? newValue)
		{
			toggleButton.Checked -= OnToggleButtonToggled;
			toggleButton.Unchecked -= OnToggleButtonToggled;
			toggleButton.Indeterminate -= OnToggleButtonToggled;

			if (newValue != null)
			{
				toggleButton.Checked += OnToggleButtonToggled;
				toggleButton.Unchecked += OnToggleButtonToggled;
				toggleButton.Indeterminate += OnToggleButtonToggled;
			}
		}

		private static void OnToggleButtonToggled(object sender, RoutedEventArgs e)
		{
			var toggleButton = (ToggleButton)sender;
			var command = GetToggleCommand(toggleButton);
			var parameter = GetToggleCommandParameter(toggleButton);
			var toggleState = toggleButton.IsChecked;
			if (command != null && command.CanExecute(toggleState, parameter))
			{
				command.Execute(toggleState, parameter);
			}
		}
	}
}
