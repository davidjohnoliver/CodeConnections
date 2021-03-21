#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Graph.Display;

namespace CodeConnections.Presentation.Converters
{
	public class DisplayEdgeTooltipConverter : ValueConverter<DisplayEdge, string?>
	{
		protected override string? ConvertInner(DisplayEdge value, object parameter, CultureInfo culture) => value switch
		{
			MultiDependencyDisplayEdge multiDependencyDisplayEdge => multiDependencyDisplayEdge.PathInfo,
			_ => null
		};

		protected override string? ConvertNull(object parameter, CultureInfo culture) => null;
	}
}
