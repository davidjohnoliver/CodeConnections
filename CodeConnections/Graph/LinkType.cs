using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Graph
{
	/// <summary>
	/// Characteristics of a dependent-dependency link.
	/// </summary>
	[Flags]
	public enum LinkType
	{
		Unspecified = 0,
		/// <summary>
		/// The dependent inherits from the dependency class.
		/// </summary>
		InheritsFromClass = 1,

		/// <summary>
		/// The dependent implements the dependency interface.
		/// </summary>
		ImplementsInterface = 2,


		/// <summary>
		/// The dependent implements or inherits from the dependency.
		/// </summary>
		InheritsOrImplements = InheritsFromClass | ImplementsInterface
	}
}
