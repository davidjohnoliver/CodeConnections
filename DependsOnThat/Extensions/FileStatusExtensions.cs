using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Git;
using LibGit2Sharp;

namespace CodeConnections.Extensions
{
	public static class FileStatusExtensions
	{
		/// <summary>
		/// True if the <see cref="FileStatus"/> indicates that this is a modified or new file, either staged in the index or unstaged in the working directory.
		/// </summary>
		public static bool IsModifiedOrNew(this FileStatus fileStatus)
			=> IsModified(fileStatus) || IsNew(fileStatus);

		private static bool IsModified(FileStatus fileStatus)
			=> fileStatus.HasFlag(FileStatus.ModifiedInIndex)
			|| fileStatus.HasFlag(FileStatus.ModifiedInWorkdir);
		private static bool IsNew(FileStatus fileStatus)
			=> fileStatus.HasFlag(FileStatus.NewInIndex)
			|| fileStatus.HasFlag(FileStatus.NewInWorkdir);

		public static GitStatus ToGitStatus(this FileStatus fileStatus)
		{
			var output = GitStatus.Unchanged;
			if (IsNew(fileStatus))
			{
				output |= GitStatus.New;
			}

			if (IsModified(fileStatus))
			{
				output |= GitStatus.Modified;
			}

			// TODO: removed, etc

			return output;
		}
	}
}
