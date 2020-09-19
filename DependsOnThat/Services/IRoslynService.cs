using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using DependsOnThat.Roslyn;
using Microsoft.CodeAnalysis;

namespace DependsOnThat.Services
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
		/// Get symbols of all types declared within the files at the supplied paths.
		/// </summary>
		IAsyncEnumerable<(string FilePath, ITypeSymbol Symbol)> GetDeclaredSymbolsFromFilePaths(IEnumerable<string> filePaths, CancellationToken ct);

		/// <summary>
		/// Get projects in the solution topologically sorted in dependency order, root dependencies first.
		/// </summary>
		public IEnumerable<ProjectIdentifier> GetSortedProjects();
	}
}