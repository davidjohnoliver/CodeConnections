using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Graph
{
	public enum IncludeActiveMode
	{
		/// <summary>
		/// Don't automatically include the active document in the graph
		/// </summary>
		DontInclude,
		/// <summary>
		/// Include the active document and all its connections in the graph
		/// </summary>
		ActiveAndConnections,
		/// <summary>
		/// Include only the active document itself in the graph
		/// </summary>
		ActiveOnly
	}
}
