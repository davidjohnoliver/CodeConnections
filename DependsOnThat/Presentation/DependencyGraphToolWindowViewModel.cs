#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using DependsOnThat.Disposables;
using DependsOnThat.Extensions;
using DependsOnThat.Graph;
using DependsOnThat.Services;
using Microsoft.VisualStudio.Threading;
using QuickGraph;

namespace DependsOnThat.Presentation
{
	internal class DependencyGraphToolWindowViewModel : ViewModelBase, IDisposable
	{
		private readonly IDocumentsService _documentsService;
		private readonly IRoslynService _roslynService;
		private readonly IGitService? _gitService;
		private readonly JoinableTaskFactory _joinableTaskFactory;
		private readonly HashSet<string> _rootDocuments = new HashSet<string>();
		private readonly SerialCancellationDisposable _graphUpdatesRegistration = new SerialCancellationDisposable();


		private IBidirectionalGraph<DisplayNode, DisplayEdge> _graph = Empty;
		private int _extensionDepth = 1;
		private bool _shouldUseGitForRoots;

		public IBidirectionalGraph<DisplayNode, DisplayEdge> Graph { get => _graph; set => OnValueSet(ref _graph, value); }

		private static IBidirectionalGraph<DisplayNode, DisplayEdge> Empty { get; } = new BidirectionalGraph<DisplayNode, DisplayEdge>();

		public int ExtensionDepth
		{
			get => _extensionDepth;
			set
			{
				if (OnValueSet(ref _extensionDepth, value))
				{
					TryUpdateGraph();
				}
			}
		}

		private DisplayNode? _selectedNode;
		public DisplayNode? SelectedNode
		{
			get => _selectedNode;
			set
			{
				if (OnValueSet(ref _selectedNode, value))
				{
					if (value?.FilePath != null)
					{
						_joinableTaskFactory.RunAsync(async () =>
						{
							await Dispatcher.Yield(); // Opening a file takes a noticeable delay, so give the visuals a chance to update first
							if (SelectedNode == value)
							{
								_documentsService.OpenFileAsPreview(value.FilePath);
							}
						});
					}
				}
			}
		}

		private TimeSpan? _graphingtime;
		public TimeSpan? GraphingTime { get => _graphingtime; set => OnValueSet(ref _graphingtime, value); }

		public ICommand AddActiveDocumentAsRootCommand { get; }
		public ICommand ClearRootsCommand { get; }
		public ICommand UseGitModifiedFilesAsRootCommand { get; }

		public DependencyGraphToolWindowViewModel(JoinableTaskFactory joinableTaskFactory, IDocumentsService documentsService, IRoslynService roslynService, IGitService? gitService)
		{
			_joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
			_documentsService = documentsService ?? throw new ArgumentNullException(nameof(documentsService));
			_documentsService.ActiveDocumentChanged += OnActiveDocumentChanged;
			_roslynService = roslynService ?? throw new ArgumentNullException(nameof(roslynService));
			_gitService = gitService;
			AddActiveDocumentAsRootCommand = SimpleCommand.Create(AddActiveDocumentAsRoot);
			ClearRootsCommand = SimpleCommand.Create(ClearRoots);
			UseGitModifiedFilesAsRootCommand = SimpleCommand.Create(UseGitModifiedFilesAsRoot);
		}

		private void AddActiveDocumentAsRoot()
		{
			var active = _documentsService.GetActiveDocument();
			if (active == null)
			{
				// Ideally the command couldn't be executed if there were no active document
				return;
			}
			_shouldUseGitForRoots = false;
			if (_rootDocuments.Add(active))
			{
				TryUpdateGraph();
			}
		}

		private void UseGitModifiedFilesAsRoot()
		{
			_shouldUseGitForRoots = true;
			TryUpdateGraph();
		}

		private void TryUpdateGraph()
		{
			GraphingTime = null;
			_joinableTaskFactory.RunAsync(async () =>
			{
				var stopwatch = Stopwatch.StartNew();
				var graph = await GetGraphAsync(_graphUpdatesRegistration.GetNewToken());
				stopwatch.Stop();
				if (graph == null)
				{
					return;
				}
				if (graph.VertexCount > 0)
				{
					GraphingTime = stopwatch.Elapsed;
				}
				Graph = graph;
			});
		}

		private Task<IBidirectionalGraph<DisplayNode, DisplayEdge>?> GetGraphAsync(CancellationToken ct)
		{
			return Task.Run(async () =>
			{
				if (ct.IsCancellationRequested)
				{
					return null;
				}

				var rootDocuments = _shouldUseGitForRoots ?
					await (_gitService?.GetAllModifiedAndNewFiles(ct) ?? throw new InvalidOperationException("Git repository is not available"))
					: _rootDocuments;

				var rootSymbols = await _roslynService.GetDeclaredSymbolsFromFilePaths(rootDocuments, ct).ToListAsync(ct);
				if (rootSymbols.Count == 0)
				{
					return Empty;
				}
				var nodeGraph = await NodeGraph.BuildGraphFromRoots(rootSymbols, _roslynService.GetCurrentSolution(), ct);
				var graph = nodeGraph.GetDisplaySubgraph(ExtensionDepth);

				if (ct.IsCancellationRequested)
				{
					return null;
				}

				return graph;
			}, ct);
		}

		private void ClearRoots()
		{
			_rootDocuments.Clear();
			_shouldUseGitForRoots = false;
			TryUpdateGraph();
		}

		private void OnActiveDocumentChanged()
		{
			var activeDocument = _documentsService.GetActiveDocument();
			if (Graph.Vertices.FirstOrDefault(dn => dn.FilePath == activeDocument) is DisplayNode activeDocumentNode)
			{
				SelectedNode = activeDocumentNode;
			}
		}

		public void Dispose()
		{
			_graphUpdatesRegistration.Dispose();
			_documentsService.ActiveDocumentChanged -= OnActiveDocumentChanged;
		}
	}
}
