using System;
using System.Collections.Generic;
using System.Text;

namespace SubjectSolution.Inheritance
{
	interface ISomeGenericInterface<T>
	{
		T Bag { get; }
	}
}
