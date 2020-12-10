#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Utilities;
using static System.Math;

namespace DependsOnThat.Text
{
	/// <summary>
	/// Formats dictionary as a GitHub-flavoured Markdown table, with nicely-padded columns.
	/// </summary>
	public class MarkdownStyleTableFormatter : IDictionaryFormatter
	{
		public IEnumerable<string> FormatDictionary<TKey, TValue>(string keyHeader, string valueHeader, IEnumerable<KeyValuePair<TKey, TValue>> dictionary)
			=> FormatTable(new[] { keyHeader, valueHeader }, dictionary.Select(kvp => new[] { kvp.Key!.ToString(), kvp.Value?.ToString() ?? "" }).ToList());

		public IEnumerable<string> FormatTable(IList<string> headers, IEnumerable<IList<string>> rows)
		{
			var noOfColumns = headers.Count;
			if (noOfColumns < 2)
			{
				throw new ArgumentOutOfRangeException(nameof(headers), "Table must have at least 2 columns.");
			}
			var columnWidths = ArrayUtils.Create(noOfColumns, _ => 5); // Min width 5 to have 3 dashes + possible alignment
			UpdateColumnWidths(headers);

			foreach (var row in rows)
			{
				if (row.Count < noOfColumns)
				{
					throw new ArgumentOutOfRangeException($"{noOfColumns} headers, but row only contained {row.Count} columns.");
				}
				UpdateColumnWidths(row);
			}
			var sb = new StringBuilder();

			yield return MarkdownRow(headers);

			yield return MarkdownRow(ArrayUtils.Create(noOfColumns, i => $" {new string('-', columnWidths[i] - 2)} "));

			foreach (var row in rows)
			{
				yield return MarkdownRow(row);
			}

			void UpdateColumnWidths(IList<string> row)
			{
				for (int i = 0; i < noOfColumns; i++)
				{
					columnWidths[i] = Max(columnWidths[i], row[i].Length + 2); // Pad the widest cell by 2
				}
			}

			string MarkdownRow(IList<string> row)
			{
				sb.Clear();
				sb.Append(Pad(row[0], columnWidths[0]));

				for (int i = 1; i < noOfColumns; i++)
				{
					sb.Append('|');
					sb.Append(Pad(row[i], columnWidths[i]));
				}
				return sb.ToString();
			}
		}

		private static string Pad(string cell, int paddedLength)
		{
			if (cell.Length == paddedLength)
			{
				return cell;
			}

			var toPad = paddedLength - cell.Length;

			var leftPad = (toPad + 1) / 2;
			var rightPad = toPad / 2;

			// TODO: support justifications
			string output = $"{new string(' ', leftPad)}{cell}{new string(' ', rightPad)}";
			if (output.Length != paddedLength)
			{
				;
			}
			return output;
		}
	}
}
