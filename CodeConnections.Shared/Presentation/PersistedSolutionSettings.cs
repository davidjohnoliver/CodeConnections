#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Graph;
using CodeConnections.Roslyn;

namespace CodeConnections.Presentation
{
	public record PersistedSolutionSettings(
		// This type is serialized - don't modify the names of existing members.

		bool IncludeGeneratedTypes,
		bool IsGitModeEnabled,
		string[]? ExcludedProjects,
		bool IncludeNestedTypes,
		bool IsImportantTypesModeEnabled,
		ImportantTypesMode ImportantTypesMode,
		IntOrAuto NumberOfImportantTypesRequested
	)
	{
	}
}
