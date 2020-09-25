using System;
using System.Collections.Generic;
using System.Text;
using SubjectSolution.Generated;

namespace SubjectSolution
{
	class SomeDeeperClass
	{
		public SomeCircularClass CircleRound { get; set; }

		public SomeClassDepth3 SomeClassDepth3;

		void DoStuff()
		{
			var implicitVar = SomeStaticClass.SomeClassAsImplicitVar;

			var generated = new SomeGeneratedClass();

			var aBitGenerated = new SomePartiallyGeneratedClass();
		}
	}
}
