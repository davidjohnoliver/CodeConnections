using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Presentation.Converters
{
	public class BooleanToDoubleConverter : ValueConverter<bool, double>
	{
		public double TrueValue { get; set; }
		public double FalseValue { get; set; }
		protected override double ConvertInner(bool value, object parameter, CultureInfo culture) => value ? TrueValue : FalseValue;

		protected override double ConvertNull(object parameter, CultureInfo culture) => FalseValue;
	}
}
