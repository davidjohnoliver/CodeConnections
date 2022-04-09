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
			ImportantTypesMode.HouseBlend => "Show top types by combined score",
			ImportantTypesMode.MostDependents => "Show top types by direct dependents (classes used a lot)",
			ImportantTypesMode.MostDependencies => "Show top types by direct dependencies (classes doing a lot)",
			ImportantTypesMode.MostLOC => "Show top types by lines of code",
			_ => ""
		};

		protected override string ConvertNull(object parameter, CultureInfo culture) => "";
	}
}
