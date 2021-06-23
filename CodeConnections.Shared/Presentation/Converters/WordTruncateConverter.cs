#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Presentation.Converters
{
	public class WordTruncateConverter : ValueConverter<object, string>
	{
		public int CharacterLimit { get; set; } = int.MaxValue;
		private const string Ellipsis = "...";

		protected override string ConvertInner(object value, object parameter, CultureInfo culture)
		{
			if (CharacterLimit < Ellipsis.Length + 2)
			{
				throw new InvalidOperationException($"{nameof(CharacterLimit)} {CharacterLimit} is too short.");
			}
			var str = value.ToString();
			if (str.Length <= CharacterLimit)
			{
				return str;
			}

			var substringLength = (CharacterLimit - Ellipsis.Length) / 2;
			return $"{str.Substring(0, substringLength)}{Ellipsis}{str.Substring(str.Length - substringLength, substringLength)}";
		}

		protected override string ConvertNull(object parameter, CultureInfo culture) => "";
	}
}
