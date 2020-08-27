#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using LibGit2Sharp;
using Microsoft.VisualStudio.Threading;

namespace DependsOnThat.Services
{
	internal class GitService : IGitService
	{
		private readonly string _repositoryPath;

		public GitService(string repositoryPath)
		{
			_repositoryPath = repositoryPath;
		}

		public Task<ICollection<string>> GetAllModifiedAndNewFiles(CancellationToken ct) => Task.Run(GetAllModifiedAndNewFilesSync, ct);

		private ICollection<string> GetAllModifiedAndNewFilesSync()
		{
			using (var repo = new Repository(_repositoryPath))
			{
				var wdPath = repo.Info.WorkingDirectory;
				var status = repo.RetrieveStatus(new StatusOptions { IncludeIgnored = false });
				return status.Where(e => e.State.IsModifiedOrNew()).Select(e => Path.GetFullPath(Path.Combine(wdPath, e.FilePath))).ToList(); // Materialize eagerly because the Repository will be disposed after the method returns
			}
		}

		public static GitService? GetServiceOrDefault(string candidatePath)
		{
			var repoPath = Repository.Discover(candidatePath);
			return repoPath != null ? new GitService(repoPath) : null;

		}
	}
}
