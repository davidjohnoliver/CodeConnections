#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Presentation
{
	public record PersistedUserSettings(int MaxAutomaticallyLoadedNodes, GraphLayoutMode LayoutMode)
	{
	}
}
