using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Utilities
{
	public static class TimeUtils
	{
		public static string GetRoundedTime(TimeSpan value, CultureInfo culture)
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
