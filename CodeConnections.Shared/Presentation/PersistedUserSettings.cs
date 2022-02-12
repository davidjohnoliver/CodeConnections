#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Graph;

namespace CodeConnections.Presentation
{
	/// <summary>
	/// Represents persisted user settings used by Code Connections.
	/// </summary>
	/// <remarks>See <see cref="Services.DialogUserSettingsService"/> for instructions on adding a new setting.</remarks>
	public record PersistedUserSettings(
		int MaxAutomaticallyLoadedNodes,
		GraphLayoutMode LayoutMode,
		bool IsActiveAlwaysIncluded,
		IncludeActiveMode IncludeActiveMode,
		OutputLevel OutputLevel,
		bool EnableDebugFeatures
	)
	{
	}
}
