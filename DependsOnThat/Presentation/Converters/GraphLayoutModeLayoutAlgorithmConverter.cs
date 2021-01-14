﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Presentation.Converters
{
	public class GraphLayoutModeLayoutAlgorithmConverter : ValueConverter<GraphLayoutMode, string?>
	{
		protected override string? ConvertInner(GraphLayoutMode value, object parameter, CultureInfo culture) => value switch
		{
			// Seems to give slightly better results than (just) 'Sugiyama'
			GraphLayoutMode.Hierarchy => "EfficientSugiyama",
			// "marginally the best of a pretty close pack of 'blobby' options" - KK and ISOM are alternatives
			GraphLayoutMode.Blob => "LinLog",
			_ => default
		};

		protected override string? ConvertNull(object parameter, CultureInfo culture) => null;
	}
}