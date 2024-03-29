﻿#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeConnections.Disposables;
using CodeConnections.Extensions;
using CodeConnections.Git;
using LibGit2Sharp;

namespace CodeConnections.Services
{
	internal class GitService : IGitService, IDisposable
	{
		private readonly ISolutionService _solutionService;
		private string? _repositoryPath;
		private Task _checkReady;
		private readonly SerialCancellationDisposable _solutionChangedRegistration = new SerialCancellationDisposable();

		public GitService(ISolutionService solutionService)
		{
			_solutionService = solutionService ?? throw new ArgumentNullException(nameof(solutionService));
			_checkReady = UpdateRepositoryPath();
			_solutionService.SolutionOpened += OnSolutionOpened;
		}

		private void OnSolutionOpened()
		{
			_checkReady = UpdateRepositoryPath();
		}

		private async Task UpdateRepositoryPath()
		{
			_repositoryPath = null;
			var candidatePath = _solutionService.GetSolutionPath();

			var ct = _solutionChangedRegistration.GetNewToken();
			var discoveredPath = candidatePath.IsNullOrEmpty() ? "" : await Task.Run(() => Repository.Discover(candidatePath), ct);
			if (!ct.IsCancellationRequested)
			{
				_repositoryPath = discoveredPath;
			}
		}

		public async Task<ICollection<GitInfo>> GetAllModifiedAndNewFiles(CancellationToken ct)
		{
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - not a foreign Task
			await _checkReady;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks

			var path = _repositoryPath;
			if (path.IsNullOrWhiteSpace())
			{
				return new GitInfo[0];
			}

			return await Task.Run(() => GetAllModifiedAndNewFilesSync(path), ct);
		}

		private ICollection<GitInfo> GetAllModifiedAndNewFilesSync(string path)
		{
			using (var repo = new Repository(path))
			{
				var wdPath = repo.Info.WorkingDirectory;
				var status = repo.RetrieveStatus(new StatusOptions { IncludeIgnored = false });
				return status.Where(e => e.State.IsModifiedOrNew()).Select(e => e.ToGitInfo(wdPath)).ToList(); // Materialize eagerly because the Repository will be disposed after the method returns
			}
		}

		public void Dispose()
		{
			_solutionService.SolutionOpened -= OnSolutionOpened;
		}
	}
}
