#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeConnections.Extensions;
using CodeConnections.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using static Microsoft.CodeAnalysis.WorkspaceChangeKind;

namespace CodeConnections.Services
{
	internal class RoslynService : IRoslynService, IModificationsService, IDisposable
	{
		private readonly VisualStudioWorkspace _workspace;

		public event Action<DocumentId>? DocumentInvalidated;
		public event Action? SolutionInvalidated;

		public RoslynService(VisualStudioWorkspace workspace)
		{
			_workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
			_workspace.WorkspaceChanged += OnWorkspaceChanged;
		}

		public Solution GetCurrentSolution() => _workspace.CurrentSolution;

		public IEnumerable<ProjectIdentifier> GetSortedProjects()
		{
			var solution = GetCurrentSolution();
			return solution
				.GetProjectDependencyGraph()
				.GetTopologicallySortedProjects()
				.Select(id => solution.GetProject(id)?.ToIdentifier() ?? throw new InvalidOperationException());
		}

		private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
		{
			switch (e.Kind)
			{
				case DocumentChanged:
				case DocumentAdded:
				case DocumentRemoved:
					DocumentInvalidated?.Invoke(e.DocumentId);
					break;
				case SolutionChanged:
				case SolutionAdded:
				case SolutionRemoved:
				case SolutionCleared:
				case SolutionReloaded:
				case ProjectAdded:
				case ProjectRemoved:
				case ProjectChanged:
				case ProjectReloaded:
					SolutionInvalidated?.Invoke();
					break;
			}
		}

		public void Dispose()
		{
			_workspace.WorkspaceChanged -= OnWorkspaceChanged;
		}
	}
}
