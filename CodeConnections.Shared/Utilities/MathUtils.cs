#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CodeConnections.Utilities
{
	public static class MathUtils
	{
		public static int Clamp(int value, int atLeast, int noMoreThan)
		{
			Debug.Assert(noMoreThan >= atLeast, "max >= min");
			return Math.Max(Math.Min(value, noMoreThan), atLeast);
		}
	}
}
