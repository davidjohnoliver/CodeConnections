using System;
using System.Collections.Generic;
using System.Text;

namespace SubjectSolution
{
	class SomeClassUsingConstructedTypes
	{
		void DoStuff()
		{
			var implicitGenericArg = SomeStaticClass.SomeClassInGenericTypeEnumerable;
			var implicitArray = SomeStaticClass.ArrayOfSomeClassInArray;

			var implicitUnnamedTuple = SomeStaticClass.UnnamedTupleEntry;
			var implicitNamedTuple = SomeStaticClass.NamedTupleEntry;
			var implicitNested = SomeStaticClass.NamedTupleNestedGenericEntry;

			var implicitGenericClosed = SomeStaticClass.SomeGenericClassClosed;
		}

		void DoGeneric<T>()
		{
			var implicitGenericOpen = SomeStaticClass.GetSomeGenericClassOpen<T>();
		}
	}
}
