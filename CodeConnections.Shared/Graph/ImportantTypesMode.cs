using System;
using System.Collections.Generic;
using System.Text;

namespace CodeConnections.Graph
{
	public enum ImportantTypesMode
	{
		/// <summary>
		/// Important types mode is disabled.
		/// </summary>
		None,
		/// <summary>
		/// Important types mode is enabled using the 'house blend' criterion.
		/// </summary>
		HouseBlend,
		/// <summary>
		/// Important types mode is enabled using the criterion of number of direct dependents.
		/// </summary>
		MostDependents,
		/// <summary>
		/// Important types mode is enabled using the criterion of number of direct dependencies.
		/// </summary>
		MostDependencies,
		/// <summary>
		/// Important types mode is enabled using the criterion of number of lines of code.
		/// </summary>
		MostLOC
	}
}
