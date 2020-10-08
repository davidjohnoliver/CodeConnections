using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Text
{
	/// <summary>
	/// Formats 'VS output window-looking' headers
	/// </summary>
	public class ConsoleHeaderFormatter : IHeaderFormatter
	{
		public string FormatHeader(string headerContent, int headerLevel)
		{
			var bookend = GetHeaderBookend(headerLevel);

			return $"{bookend} {headerContent} {bookend}";
		}

		private string GetHeaderBookend(int headerLevel)
		{
			if (headerLevel == 1)
			{
				return "==========";
			}

			if (headerLevel > 5)
			{
				throw new NotSupportedException("Header level not supported");
			}

			const string baseSubHeader = "----------";
			return baseSubHeader[0..^(3 * (headerLevel - 2))];
		}
	}
}
