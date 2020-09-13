using System;
using System.Collections.Generic;
using System.Text;

namespace SubjectSolution
{
	static class SomeStaticClass
	{
		public static SomeClassAsImplicitVar SomeClassAsImplicitVar;
		public static IEnumerable<SomeClassInGenericType> SomeClassInGenericTypeEnumerable;
		public static SomeClassInArray[] ArrayOfSomeClassInArray;

		public static (int, SomeClassInTuple1) UnnamedTupleEntry;
		public static (string Title, SomeClassInTuple2 Content) NamedTupleEntry;
		public static (int No, string Strings, List<IComparable<SomeClassInTuple3>[]> Apollo) NamedTupleNestedGenericEntry;

		public static SomeGenericClass1<T> GetSomeGenericClassOpen<T>() => throw new NotImplementedException();
		public static SomeGenericClass2<string> SomeGenericClassClosed;
	}
}
