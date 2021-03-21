#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Utilities;

namespace CodeConnections.Presentation.Converters
{
	public class OperationTimeTextConverter : ValueConverter<TimeSpan?, string>
	{
		public string OperationType { get; set; } = string.Empty;

		protected override string ConvertInner(TimeSpan? nullableValue, object parameter, CultureInfo culture)
		{
			if (!(nullableValue is TimeSpan value))
			{
				return "...";
			}

			return $"{OperationType} took about {GetFormattedTime(value, culture)}";
		}

		protected override string ConvertNull(object parameter, CultureInfo culture) => ConvertInner(null, parameter, culture);

		private string GetFormattedTime(TimeSpan value, CultureInfo culture) => TimeUtils.GetRoundedTime(value, culture);
	}
}
