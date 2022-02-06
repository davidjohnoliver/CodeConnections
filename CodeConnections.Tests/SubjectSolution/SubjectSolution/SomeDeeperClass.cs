﻿using System;
using System.Collections.Generic;
using System.Text;
using SubjectSolution.Generated;
using SubjectSolution.Inheritance;
using SubjectSolution.MultiTypeFiles;

namespace SubjectSolution
{
	class SomeDeeperClass
	{
		public SomeCircularClass CircleRound { get; set; }

		public SomeClassDepth3 SomeClassDepth3;

		public SomeClassSelfReference SomeClassSelfReference;

		public SomeBaseClass SomeBaseClass;

		public SomeMultiRoot someMultiRoot;

		void DoStuff()
		{
			var implicitVar = SomeStaticClass.SomeClassAsImplicitVar;

			var generated = new SomeGeneratedClass();

			var aBitGenerated = new SomePartiallyGeneratedClass();
		}
	}
}
