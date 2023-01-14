#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CodeConnections.Presentation
{
	/// <summary>
	/// Simple <see cref="ICommand"/> implementation which takes a synchronous type-checked delegate and exposes a boolean flag for CanExecute.
	/// </summary>
	public class SimpleCommand<T> : ICommand
	{
		private readonly Action<T?> _execute;
		private readonly T? _defaultValue;
		private bool _canExecute = true;

		public SimpleCommand(Action<T?> execute, T? defaultValue)
		{
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_defaultValue = defaultValue;
		}

		public bool CanExecute
		{
			get => _canExecute;
			set
			{
				var wasChanged = _canExecute != value;
				_canExecute = value;
				if (wasChanged)
				{
					CanExecuteChanged?.Invoke(this, new EventArgs());
				}
			}
		}

		public event EventHandler? CanExecuteChanged;

		bool ICommand.CanExecute(object parameter) => CanExecute;

		void ICommand.Execute(object parameter)
		{
			if (parameter is T t)
			{
				_execute(t);
			}
			else if (parameter == null)
			{
				_execute(_defaultValue);
			}
		}
	}

	public static class SimpleCommand
	{

		public static SimpleCommand<T> Create<T>(Action<T?> execute) where T : class => new SimpleCommand<T>(execute, default);

		public static SimpleCommand<T> Create<T>(Action<T> execute, T defaultWhenNull) where T : struct => new SimpleCommand<T>(execute, defaultWhenNull);

		public static SimpleCommand<object> Create(Action execute)
		{
			if (execute is null)
			{
				throw new ArgumentNullException(nameof(execute));
			}

			return new SimpleCommand<object>(ExecuteWrapper, null);

			void ExecuteWrapper(object? _)
			{
				execute();
			}
		}
	}
}
