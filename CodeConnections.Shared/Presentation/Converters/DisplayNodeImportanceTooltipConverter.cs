#nullable enable

using CodeConnections.Graph;
using CodeConnections.Graph.Display;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using static CodeConnections.Graph.ImportantTypesMode;

namespace CodeConnections.Presentation.Converters
{
	internal class DisplayNodeImportanceTooltipConverter : MultiValueConverter<ImportantTypesMode?, DisplayNode, string?>
	{
		protected override string? ConvertInner(ImportantTypesMode? mode, DisplayNode displayNode, object parameter, CultureInfo culture)
		{
			mode ??= None;
			var nl = Environment.NewLine;

			return mode switch
			{
				MostDependents => $"{displayNode.NumberOfDependents} dependents",
				MostDependencies => $"{displayNode.NumberOfDependencies} dependencies",
				MostLOC => $"{displayNode.LinesOfCode} lines",
				HouseBlend => $"{displayNode.NumberOfDependents} dependents{nl}{displayNode.NumberOfDependencies} dependencies{nl}{displayNode.LinesOfCode} LOC{nl}{displayNode.CombinedImportanceScore:F2} combined score",
				_ => null
			};
		}
	}
}
