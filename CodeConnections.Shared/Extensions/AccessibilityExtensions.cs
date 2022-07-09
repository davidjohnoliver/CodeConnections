using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeConnections.Extensions
{
	public static class AccessibilityExtensions
	{
		public static bool IsPubliclyVisible(this Accessibility accessibility)
		{
			switch (accessibility)
			{
				case Accessibility.Protected:
				case Accessibility.ProtectedOrInternal:
				case Accessibility.Public:
					return true;
				default:
					return false;
			}
		}
	}
}
