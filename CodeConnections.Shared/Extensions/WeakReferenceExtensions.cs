#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace CodeConnections.Extensions
{
	public static class WeakReferenceExtensions
	{
		public static T? TargetOrDefault<T>(this WeakReference<T> weakRef) where T : class
		{
			if (weakRef.TryGetTarget(out T target))
			{
				return target;
			}

			return default;
		}
	}
}
