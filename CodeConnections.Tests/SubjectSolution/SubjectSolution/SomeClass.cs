using SubjectSolution.Core;
using SubjectSolution.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubjectSolution
{
	class SomeClass
	{
		public SomeOtherClass SomeOtherClass { get; set; }

		public string StringProp { get; set; }

		public void ModifyStringProp()
		{
			StringProp = StringProp.UpgradeString();
		}

		public IEnumerable<SomeEnumeratedClass> GetEnumerated() => throw new NotImplementedException();

		public SomeClassCore UpstreamProperty { get; set; }
	}
}
