#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Utilities
{
	public static class SetUtils
	{
		public static ISet<T> GetEmpty<T>()
		{
			// TODO: this should return a cached, read-only set
			return new HashSet<T>();
		}
	}
}
