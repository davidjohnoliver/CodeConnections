using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Presentation
{
	/// <summary>
	/// Different user-facing options for graph layout algorithm.
	/// </summary>
	public enum GraphLayoutMode
	{
		/// <summary>
		/// Layout graph with a vertical hierarchy
		/// </summary>
		Hierarchy,
		/// <summary>
		/// Layout graph for maximum space efficiency
		/// </summary>
		Blob
	}
}
