#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DependsOnThat.Disposables;
using DependsOnThat.Graph;
using DependsOnThat.Services;
using Microsoft.VisualStudio.Threading;

namespace DependsOnThat.Presentation
{
	internal class DependencyGraphToolWindowViewModel
	{
		private readonly IDocumentsService _documentsService;
		private readonly IRoslynService _roslynService;
		private readonly JoinableTaskFactory _joinableTaskFactory;
		private readonly HashSet<string> _rootDocuments = new HashSet<string>();
		private readonly SerialDisposable _graphUpdatesRegistration = new SerialDisposable();

		private NodeGraph? _graph;

		public ICommand AddActiveDocumentAsRootCommand { get; }
		public ICommand ClearRootsCommand { get; }

		public DependencyGraphToolWindowViewModel(JoinableTaskFactory joinableTaskFactory, IDocumentsService documentsService, IRoslynService roslynService)
		{
			_joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
			_documentsService = documentsService ?? throw new ArgumentNullException(nameof(documentsService));
			_roslynService = roslynService ?? throw new ArgumentNullException(nameof(roslynService));
			AddActiveDocumentAsRootCommand = SimpleCommand.Create(AddActiveDocumentAsRoot);
			ClearRootsCommand = SimpleCommand.Create(ClearRoots);
		}

		private void AddActiveDocumentAsRoot()
		{
			var active = _documentsService.GetActiveDocument();
			if (active == null)
			{
				// Ideally the command couldn't be executed if there were no active document
				return;
			}
			if (_rootDocuments.Add(active))
			{
				TryUpdateGraph();
			}
		}

		private void TryUpdateGraph()
		{
			if (_rootDocuments.Count > 0)
			{
				_joinableTaskFactory.RunAsync(async () =>
				{
					var cd = new CancellationDisposable();
					_graphUpdatesRegistration.Disposable = cd;
					var ct = cd.Token;
					var rootSymbols = await _roslynService.GetDeclaredSymbolsFromFilePaths(_rootDocuments, ct).ToListAsync(ct);
					_graph = await NodeGraph.BuildGraphFromRoots(rootSymbols, _roslynService.GetCurrentSolution(), ct);
				});
			}
			else
			{
				_graph = null;
			}
		}

		private void ClearRoots()
		{
			_rootDocuments.Clear();
			TryUpdateGraph();
		}
	}
}
