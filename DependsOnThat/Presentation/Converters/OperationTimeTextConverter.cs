#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Presentation.Converters
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

		private string GetFormattedTime(TimeSpan value, CultureInfo culture)
		{
			if (value.TotalSeconds > 10)
			{
				return $"{value.TotalSeconds:F0} s";
			}
			else if (value.TotalSeconds > 1)
			{
				var time = value.TotalSeconds.ToString("F1", culture);
				return $"{time} s";
			}
			else
			{
				return $"{value.TotalMilliseconds:F0} ms";
			}
		}
	}
}
