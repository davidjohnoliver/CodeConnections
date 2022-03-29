#nullable enable

using CodeConnections.Graph;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Media;

namespace CodeConnections.Presentation.Converters
{
	public class ImportanceToBrushConverter : ValueConverter<Importance, Brush?>
	{
		public Brush? Gold { get; set; }
		public Brush? Silver { get; set; }
		public Brush? Bronze { get; set; }

		protected override Brush? ConvertInner(Importance value, object parameter, CultureInfo culture) => value switch
		{
			Importance.High => Gold,
			Importance.Intermediate => Silver,
			Importance.Low => Bronze,
			_ => null
		};

		protected override Brush? ConvertNull(object parameter, CultureInfo culture) => null;
	}
}
