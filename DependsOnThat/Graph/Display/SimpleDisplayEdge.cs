using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Graph.Display
{
	public class SimpleDisplayEdge : DisplayEdge
	{
		public SimpleDisplayEdge(DisplayNode source, DisplayNode target) : base(source, target) { }
	}
}
