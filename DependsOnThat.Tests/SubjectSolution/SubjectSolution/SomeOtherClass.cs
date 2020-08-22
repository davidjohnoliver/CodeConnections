using System;
using System.Collections.Generic;
using System.Text;

namespace SubjectSolution
{
	class SomeOtherClass
	{
		private void DoStuff()
		{
			var array = new SomeClassInArray[7];
			array.ToString();
		}

		public void TypeConstraintDependence<T>() where T : SomeDeeperClass
		{

		}
	}
}
