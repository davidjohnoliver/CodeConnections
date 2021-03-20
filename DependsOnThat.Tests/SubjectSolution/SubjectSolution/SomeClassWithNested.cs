using System;
using System.Collections.Generic;
using System.Text;

namespace SubjectSolution
{
	class SomeClassWithNested
	{
		private NestedClass1 _nestedClass;

		private NestedClass2.NestedInNested1 _nestedInNested;

		class NestedClass1
		{

		}

		class NestedClass2
		{
			public class NestedInNested1
			{

			}
		}
	}
}
