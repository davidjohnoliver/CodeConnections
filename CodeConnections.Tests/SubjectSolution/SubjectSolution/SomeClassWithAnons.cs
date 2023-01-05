using System;
using System.Collections.Generic;
using System.Text;

namespace SubjectSolution
{
	class SomeClassWithAnons
	{
		private void AnonymousClub()
		{
			var anon = new
			{
				Blart = "Blart",
				Seven = 7
			};
		}

		private void NameBadgesWithQuestionMarks()
		{
			var wrapped = new
			{
				Deep = new SomeDeeperClass()
			};
		}
	}
}
