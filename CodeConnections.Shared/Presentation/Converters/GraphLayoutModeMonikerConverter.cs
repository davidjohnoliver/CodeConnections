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
	public class GraphLayoutModeMonikerConverter : ValueConverter<GraphLayoutMode, ImageMoniker>
	{
		protected override ImageMoniker ConvertInner(GraphLayoutMode value, object parameter, CultureInfo culture) => value switch
		{
			GraphLayoutMode.Hierarchy => KnownMonikers.Diagram,
			GraphLayoutMode.Compact => KnownMonikers.Hub,
			_ => default
		};

		protected override ImageMoniker ConvertNull(object parameter, CultureInfo culture) => default;
	}
}
