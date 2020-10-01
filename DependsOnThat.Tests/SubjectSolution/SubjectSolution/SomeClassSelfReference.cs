using System;
using System.Collections.Generic;
using System.Text;

namespace SubjectSolution
{
	class SomeClassSelfReference
	{
		public SomeClassSelfReference() // The ctor isn't picked up as a self-reference (as of current logic anyway)
		{
			Console.WriteLine($"Constructing {nameof(SomeClassSelfReference)}"); // This is a self-reference
		}

		public void SomeMethod()
		{
			SomeInnerMethod(); // This is picked up as a self-reference
		}

		private void SomeInnerMethod() { }
	}
}
