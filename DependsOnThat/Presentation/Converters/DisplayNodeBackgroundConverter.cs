#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using DependsOnThat.Graph.Display;

namespace DependsOnThat.Presentation.Converters
{
	public class DisplayNodeBackgroundConverter : ValueConverter<DisplayNode, Brush?>
	{
		public Brush? DefaultBrush { get; set; }

		protected override Brush? ConvertInner(DisplayNode value, object parameter, CultureInfo culture) => DefaultBrush;

		protected override Brush? ConvertNull(object parameter, CultureInfo culture) => null;
	}
}
