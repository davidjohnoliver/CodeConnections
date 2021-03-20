#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Roslyn;

namespace DependsOnThat.Presentation
{
	public record PersistedSolutionSettings(
		bool IncludeGeneratedTypes,
		bool IsGitModeEnabled,
		string[]? ExcludedProjects,
		bool IsActiveAlwaysIncluded, // TODO: this should go in per-user settings
		bool IncludeNestedTypes
	)
	{
	}
}
