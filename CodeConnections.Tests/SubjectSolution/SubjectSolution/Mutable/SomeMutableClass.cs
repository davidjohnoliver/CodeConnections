using System;
using System.Collections.Generic;
using System.Text;

namespace SubjectSolution.Mutable
{
	class SomeMutableClass
	{
		public void MightBeRemoved()
		{
			var removable = new SomeClassRemovableReference();
		}

		public void MightBeReplaced()
		{
			//var replacement = new SomeClassAddableReference();
		}
	}
}
