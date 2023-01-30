using CodeConnections.Export;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CodeConnections.Presentation.Converters
{
	public class ExportOptionToLabelConverter : ValueConverter<ExportOption, string>
	{
		protected override string ConvertInner(ExportOption value, object parameter, CultureInfo culture) => value switch
		{
			ExportOption.BitmapFile => "Export bitmap to PNG file",
			ExportOption.BitmapClipboard => "Export bitmap to clipboard",
			ExportOption.Mermaid => "Export Mermaid diagram",
			_ => "Unsupported"
		};

		protected override string ConvertNull(object parameter, CultureInfo culture) => null;
	}
}
