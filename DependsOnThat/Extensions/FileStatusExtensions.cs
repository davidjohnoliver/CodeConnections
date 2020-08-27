using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace DependsOnThat.Extensions
{
	public static class FileStatusExtensions
	{
		/// <summary>
		/// True if the <see cref="FileStatus"/> indicates that this is a modified or new file, either staged in the index or unstaged in the working directory.
		/// </summary>
		public static bool IsModifiedOrNew(this FileStatus fileStatus)
			=> fileStatus.HasFlag(FileStatus.ModifiedInIndex)
			|| fileStatus.HasFlag(FileStatus.ModifiedInWorkdir)
			|| fileStatus.HasFlag(FileStatus.NewInIndex)
			|| fileStatus.HasFlag(FileStatus.NewInWorkdir);
	}
}
