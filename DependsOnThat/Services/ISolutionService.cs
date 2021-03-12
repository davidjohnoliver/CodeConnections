#nullable enable

using System;

namespace DependsOnThat.Services
{
	/// <summary>
	/// Information and callbacks for the open solution.
	/// </summary>
	internal interface ISolutionService
	{
		/// <summary>
		/// Raised whenever a solution is opened.
		/// </summary>
		event Action SolutionOpened;

		/// <summary>
		/// Raised whenever a solution is closed.
		/// </summary>
		event Action SolutionClosed;

		/// <summary>
		/// Gets the full path to the current solution.
		/// </summary>
		/// <returns>Path to the current solution, or an empty string if no solution is open.</returns>
		string GetSolutionPath();
	}
}