using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace DependsOnThat.Presentation.Converters
{
	public class DisplayNodeBackgroundConverter : ValueConverter<DisplayNode, Brush>
	{
		public Brush DefaultBrush { get; set; }
		public Brush RootNodeBrush { get; set; }

		protected override Brush ConvertInner(DisplayNode value, object parameter, CultureInfo culture) => value.IsRoot ? RootNodeBrush : DefaultBrush;
	}
}
