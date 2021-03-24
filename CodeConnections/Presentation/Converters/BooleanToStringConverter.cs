#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Presentation.Converters
{
	public class BooleanToStringConverter : ValueConverter<bool, string?>
	{
		public string? TrueValue { get; set; }
		public string? FalseValue { get; set; }
		public string? NullValue { get; set; }

		protected override string? ConvertInner(bool value, object parameter, CultureInfo culture) => value ? TrueValue : FalseValue;

		protected override string? ConvertNull(object parameter, CultureInfo culture) => NullValue;
	}
}
