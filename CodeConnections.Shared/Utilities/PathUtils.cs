#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Utilities
{
	public static class PathUtils
	{
		/// <summary>
		/// Are <paramref name="path1"/> and <paramref name="path2"/> equivalent?
		/// </summary>
		/// <remarks>If either *or both* paths are null, the method will return false.</remarks>
		public static bool AreEquivalent(string? path1, string? path2)
		{
			if (path1 == null || path2 == null)
			{
				return false;
			}

			var full1 = Path.GetFullPath(path1);
			var full2 = Path.GetFullPath(path2);
			return full1.Equals(full2, StringComparison.OrdinalIgnoreCase);
		}
	}
}
