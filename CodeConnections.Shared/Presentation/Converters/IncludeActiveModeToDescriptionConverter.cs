using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Graph;

namespace CodeConnections.Presentation.Converters
{
	public class IncludeActiveModeToDescriptionConverter : ValueConverter<IncludeActiveMode, string>
	{
		protected override string ConvertInner(IncludeActiveMode value, object parameter, CultureInfo culture) => value switch
		{
			IncludeActiveMode.ActiveOnly => "Include active document",
			IncludeActiveMode.ActiveAndConnections => "Include active document and its connections",
			_ => ""
		};

		protected override string ConvertNull(object parameter, CultureInfo culture) => "";
	}
}
