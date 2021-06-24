#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace CodeConnections.Presentation.Converters
{
	public class GraphLayoutModeTooltipConverter : ValueConverter<GraphLayoutMode, string?>
	{
		protected override string? ConvertInner(GraphLayoutMode value, object parameter, CultureInfo culture) => value switch
		{
			GraphLayoutMode.Hierarchy => "Hierarchy: Layout graph with dependencies above and dependents below",
			GraphLayoutMode.Compact => "Compact: Layout graph in a space-efficient way",
			_ => default
		};

		protected override string? ConvertNull(object parameter, CultureInfo culture) => null;
	}
}
