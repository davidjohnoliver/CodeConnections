#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using CodeConnections.Graph.Display;

namespace CodeConnections.Presentation.Converters
{
	public class DisplayEdgeStrokeDashArrayConverter : ValueConverter<DisplayEdge, DoubleCollection?>
	{
		public DoubleCollection? MultiStrokeDashArray { get; set; }

		protected override DoubleCollection? ConvertInner(DisplayEdge value, object parameter, CultureInfo culture) => value switch
		{
			MultiDependencyDisplayEdge multi => MultiStrokeDashArray,
			_ => null,
		};

		protected override DoubleCollection? ConvertNull(object parameter, CultureInfo culture) => null;
	}
}
