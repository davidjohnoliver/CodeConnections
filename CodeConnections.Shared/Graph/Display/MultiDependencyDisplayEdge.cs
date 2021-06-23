using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Graph.Display
{
	/// <summary>
	/// 
	/// </summary>
	public class MultiDependencyDisplayEdge : DisplayEdge
	{
		public MultiDependencyDisplayEdge(DisplayNode source, DisplayNode target, string pathInfo) : base(source, target)
		{
			PathInfo = pathInfo;
		}

		public string PathInfo { get; }
	}
}
