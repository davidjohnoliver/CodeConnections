using System;
using System.Collections.Generic;
using System.Text;

namespace SubjectSolution.Inheritance
{
	abstract class SomeBaseClass : ISomeInheritedInterface, ISomeStillOtherInterface, ISomeGenericInterface<string>
	{
		public string Bag => throw new NotImplementedException();
	}
}
