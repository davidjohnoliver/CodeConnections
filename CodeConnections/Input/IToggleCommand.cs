#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Input
{
	/// <summary>
	/// Defines a command associated with a toggleable element.
	/// </summary>
	public interface IToggleCommand
	{
		/// <summary>
		/// Occurs when changes occur that affect whether or not the command should execute.
		/// </summary>
		event EventHandler? CanExecuteChanged;

		/// <summary>
		/// Defines the method that determines whether the command can execute in its current state.
		/// </summary>
		/// <param name="toggleState">The current state of the element (true, false, indeterminate).</param>
		/// <param name="parameter">
		/// Data used by the command. If the command does not require data to be passed, this object can be set to null.
		/// </param>
		/// <returns>true if this command can be executed; otherwise, false.</returns>
		bool CanExecute(bool? toggleState, object? parameter);

		/// <summary>
		/// Defines the method to be called when the command is invoked.
		/// </summary>
		/// <param name="toggleState">The current state of the element (true, false, indeterminate).</param>
		/// <param name="parameter">
		/// Data used by the command. If the command does not require data to be passed, this object can be set to null.
		/// </param>
		void Execute(bool? toggleState, object? parameter);
	}
}
