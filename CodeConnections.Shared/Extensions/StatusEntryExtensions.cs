﻿#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Git;
using LibGit2Sharp;

namespace CodeConnections.Extensions
{
	public static class StatusEntryExtensions
	{
		public static GitInfo ToGitInfo(this StatusEntry statusEntry, string workingDirectoryPath)
		{
			var path = Path.GetFullPath(Path.Combine(workingDirectoryPath, statusEntry.FilePath));
			return new GitInfo(path, statusEntry.State.ToGitStatus());
		}
	}
}
