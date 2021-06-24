#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace CodeConnections.Services
{
	/// <summary>
	/// Raises events whenever the state of the solution is modified.
	/// </summary>
	internal interface IModificationsService
	{
		/// <summary>
		/// Raised whenever a document is modified in any way.
		/// </summary>
		event Action<DocumentId> DocumentInvalidated;

		/// <summary>
		/// Raised whenever the solution changes or the current solution is modified in a fundamental way.
		/// </summary>
		event Action SolutionInvalidated;
	}
}
