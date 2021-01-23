#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Git
{
	[Flags]
	public enum GitStatus
	{
		Unchanged = 0,
		Modified = 1,
		New = 2,
	}
}
