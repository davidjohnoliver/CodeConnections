using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Graph;

namespace CodeConnections.Presentation.Converters
{
	public class ImportantTypesModeToDescriptionConverter : ValueConverter<ImportantTypesMode, string>
	{
		protected override string ConvertInner(ImportantTypesMode value, object parameter, CultureInfo culture) => value switch
		{
			ImportantTypesMode.HouseBlend => "House Blend - Show key classes according to a combined score",
			ImportantTypesMode.MostDependents => "Show classes with the most direct dependents (heavily-used classes)",
			ImportantTypesMode.MostDependencies => "Show types with the most direct dependencies (functionality-dense classes)",
			ImportantTypesMode.MostLOC => "Show types with the most lines of code",
			_ => ""
		};

		protected override string ConvertNull(object parameter, CultureInfo culture) => "";
	}
}
