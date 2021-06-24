#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Input;

namespace CodeConnections.Presentation
{
	public class SimpleToggleCommand<T> : IToggleCommand where T : class
	{
		private readonly Action<bool?, T?> _execute;
		private bool _canExecute = true;

		public SimpleToggleCommand(Action<bool?, T?> execute)
		{
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
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

		bool IToggleCommand.CanExecute(bool? toggleState, object? parameter) => CanExecute;

		void IToggleCommand.Execute(bool? toggleState, object? parameter)
		{
			if (parameter is T t)
			{
				_execute(toggleState, t);
			}
			else if (parameter == null)
			{
				_execute(toggleState, null);
			}
		}
	}

	public static class SimpleToggleCommand
	{

		public static SimpleToggleCommand<T> Create<T>(Action<bool?, T?> execute) where T : class => new SimpleToggleCommand<T>(execute);

		public static SimpleToggleCommand<object> Create(Action<bool?> execute)
		{
			if (execute is null)
			{
				throw new ArgumentNullException(nameof(execute));
			}

			return new SimpleToggleCommand<object>(ExecuteWrapper);

			void ExecuteWrapper(bool? toggleState, object? _)
			{
				execute(toggleState);
			}
		}
	}
}
