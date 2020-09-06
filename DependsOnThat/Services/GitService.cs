#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Disposables;
using DependsOnThat.Extensions;
using LibGit2Sharp;

namespace DependsOnThat.Services
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
			_solutionService.SolutionChanged += OnSolutionChanged;
		}

		private void OnSolutionChanged()
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

		public async Task<ICollection<string>> GetAllModifiedAndNewFiles(CancellationToken ct)
		{
			await _checkReady;

			var path = _repositoryPath;
			if (path.IsNullOrWhiteSpace())
			{
				return new string[0];
			}

			return await Task.Run(() => GetAllModifiedAndNewFilesSync(path), ct);
		}

		private ICollection<string> GetAllModifiedAndNewFilesSync(string path)
		{
			using (var repo = new Repository(path))
			{
				var wdPath = repo.Info.WorkingDirectory;
				var status = repo.RetrieveStatus(new StatusOptions { IncludeIgnored = false });
				return status.Where(e => e.State.IsModifiedOrNew()).Select(e => Path.GetFullPath(Path.Combine(wdPath, e.FilePath))).ToList(); // Materialize eagerly because the Repository will be disposed after the method returns
			}
		}

		public void Dispose()
		{
			_solutionService.SolutionChanged -= OnSolutionChanged;
		}
	}
}
