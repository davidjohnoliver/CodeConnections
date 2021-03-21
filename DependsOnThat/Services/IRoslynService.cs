using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using CodeConnections.Roslyn;
using Microsoft.CodeAnalysis;

namespace CodeConnections.Services
{
	/// <summary>
	/// Exposes methods for extracting information via the Roslyn compiler API.
	/// </summary>
	internal interface IRoslynService
	{
		/// <summary>
		/// Get the <see cref="Solution"/> object for the open solution in its current state.
		/// </summary>
		public Solution GetCurrentSolution();

		/// <summary>
		/// Get projects in the solution topologically sorted in dependency order, root dependencies first.
		/// </summary>
		public IEnumerable<ProjectIdentifier> GetSortedProjects();
	}
}