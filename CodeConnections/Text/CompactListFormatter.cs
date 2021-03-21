#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Text
{
	public class CompactListFormatter : ICompactListFormatter
	{
		private readonly string? _prefix;
		private readonly string? _suffix;
		private readonly string _separator;

		public CompactListFormatter(string? prefix, string? suffix, string separator)
		{
			_prefix = prefix;
			_suffix = suffix;
			_separator = separator ?? throw new ArgumentNullException(nameof(separator));
		}
		public string FormatList<T>(IEnumerable<T> list)
		{
			var sb = new StringBuilder();
			if (_prefix != null)
			{
				sb.Append(_prefix);
			}

			if (list.Any())
			{
				sb.Append(list.First());

				foreach (var item in list.Skip(1))
				{
					sb.Append(_separator);
					sb.Append(item);
				}
			}

			if (_suffix != null)
			{
				sb.Append(_suffix);
			}

			return sb.ToString();
		}

		private static CompactListFormatter? _openCommaSeparated;
		private static CompactListFormatter? _curlyCommaSeparated;
		public static CompactListFormatter OpenCommaSeparated => _openCommaSeparated ??= new CompactListFormatter(prefix: null, suffix: null, ", ");
		public static CompactListFormatter CurlyCommaSeparated => _curlyCommaSeparated ??= new CompactListFormatter("{ ", " }", ", ");
	}
}
