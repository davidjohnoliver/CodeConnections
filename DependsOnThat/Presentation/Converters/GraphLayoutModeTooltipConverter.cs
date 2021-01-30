#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace DependsOnThat.Presentation.Converters
{
	public class GraphLayoutModeTooltipConverter : ValueConverter<GraphLayoutMode, string?>
	{
		protected override string? ConvertInner(GraphLayoutMode value, object parameter, CultureInfo culture) => value switch
		{
			GraphLayoutMode.Hierarchy => "Vertical Dependency Hierarchy",
			GraphLayoutMode.Blob => "Space-efficient Packing",
			_ => default
		};

		protected override string? ConvertNull(object parameter, CultureInfo culture) => null;
	}
}
