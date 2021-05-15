#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Graph;

namespace CodeConnections.Presentation
{
	public record PersistedUserSettings(
		int MaxAutomaticallyLoadedNodes,
		GraphLayoutMode LayoutMode,
		bool IsActiveAlwaysIncluded,
		IncludeActiveMode IncludeActiveMode,
		OutputLevel OutputLevel
	)
	{
	}
}
