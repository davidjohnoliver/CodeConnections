#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Disposables;
using DependsOnThat.Extensions;
using DependsOnThat.Graph;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Threading;

namespace DependsOnThat.Roslyn
{
	/// <summary>
	/// Caches <see cref="SemanticModel"/>s and other Roslyn compilation data for reuse within a specific time window.
	/// </summary>
	public sealed class CompilationCache
	{
		private readonly object _gate = new object();
		private bool _isActive = false;

		private Solution? _solution;
		private readonly Dictionary<Project, Compilation?> _cachedCompilations = new Dictionary<Project, Compilation?>();
		private readonly Dictionary<SyntaxTree, SemanticModel?> _cachedSemanticModels = new Dictionary<SyntaxTree, SemanticModel?>();
		private readonly Dictionary<TypeNode, (INamedTypeSymbol?, Compilation?)> _cachedTypeSymbols = new Dictionary<TypeNode, (INamedTypeSymbol?, Compilation?)>();
		private CancellationDisposable? _cancellationDisposable;

		/// <summary>
		/// Activates the cache using <paramref name="solution"/>.
		/// </summary>
		public void Activate(Solution solution)
		{
			if (solution is null)
			{
				throw new ArgumentNullException(nameof(solution));
			}

			lock (_gate)
			{
				if (_isActive)
				{
					throw new InvalidOperationException($"{this} is already active.");
				}

				_isActive = true;
				_solution = solution;
				_cancellationDisposable = new CancellationDisposable();
			}
		}

		/// <summary>
		/// Deactivates the cache. Held values will be released.
		/// </summary>
		public void Reset()
		{
			IDisposable? cd = null;
			lock (_gate)
			{
				if (!_isActive)
				{
					throw new InvalidOperationException($"{this} is already active.");
				}

				_isActive = false;
				_solution = null;
				_cachedCompilations.Clear();
				_cachedSemanticModels.Clear();
				cd = _cancellationDisposable;
				_cancellationDisposable = null;
			}

			cd?.Dispose();
		}

		private async Task<Compilation?> GetCompilation(ProjectIdentifier projectIdentifier, CancellationToken ct)
		{
			Solution? solution = null;

			lock (_gate)
			{
				if (!_isActive)
				{
					return null;
				}
				if (_solution == null)
				{
					throw new InvalidOperationException();
				}
				solution = _solution;
				ct = GetCombined(ct);
			}

			var project = solution.GetProject(projectIdentifier.Id);

			if (project == null)
			{
				return null;
			}

			lock (_gate)
			{
				if (_cachedCompilations.TryGetValue(project, out var cached))
				{
					return cached;
				}
			}

			var compilation = await project.GetCompilationAsync(ct);
			lock (_gate)
			{
				if (_isActive)
				{
					_cachedCompilations[project] = compilation;
				}
			}

			return compilation;
		}


		/// <summary>
		/// Gets a semantic model, using the cache if possible, for <paramref name="syntaxRoot"/>.
		/// </summary>
		/// <param name="projectIdentifier">The project which <paramref name="syntaxRoot"/> belongs to.</param>
		/// <returns>The semantic model, if the cache is active and a model is found, or else null.</returns>
		public Task<SemanticModel?> GetSemanticModel(SyntaxNode syntaxRoot, ProjectIdentifier projectIdentifier, CancellationToken ct)
			=> GetSemanticModel(syntaxRoot.SyntaxTree, projectIdentifier, ct);

		private async Task<SemanticModel?> GetSemanticModel(SyntaxTree syntaxTree, ProjectIdentifier projectIdentifier, CancellationToken ct)
		{
			lock (_gate)
			{
				if (!_isActive)
				{
					return null;
				}

				if (_cachedSemanticModels.TryGetValue(syntaxTree, out var cached))
				{
					return cached;
				}

				ct = GetCombined(ct);
			}

			var compilation = await GetCompilation(projectIdentifier, ct);

			var model = compilation?.GetSemanticModel(syntaxTree);

			lock (_gate)
			{
				if (_isActive)
				{
					_cachedSemanticModels[syntaxTree] = model;
				}
			}

			return model;
		}

		private CancellationToken GetCombined(CancellationToken ct)
		{
			if (_cancellationDisposable == null)
			{
				throw new InvalidOperationException();
			}
			ct = _cancellationDisposable.Token.CombineWith(ct).Token;
			return ct;
		}

		/// <summary>
		/// Gets the type symbol corresponding to <paramref name="node"/>, as well as the compilation it belongs to. Returns null for both 
		/// if cache is inactive, and null for either if not found.
		/// </summary>
		public async Task<(INamedTypeSymbol? Symbol, Compilation? Compilation)> GetSymbolForNode(Node node, CancellationToken ct)
		{
			if (node is TypeNode typeNode)
			{
				lock (_gate)
				{
					if (!_isActive)
					{
						return default;
					}

					if (_cachedTypeSymbols.TryGetValue(typeNode, out var cached))
					{
						return cached;
					}

					ct = GetCombined(ct);
				}

				var project = GetContainingProject(typeNode);

				if (project == null)
				{
					return default;
				}

				var compilation = await GetCompilation(project.ToIdentifier(), ct);

				if (compilation == null)
				{
					return default;
				}

				var symbol = compilation.GetTypeByMetadataName(typeNode.FullMetadataName);

				lock (_gate)
				{
					if (_isActive)
					{
						_cachedTypeSymbols[typeNode] = (symbol, compilation);
					}
				}

				return (symbol, compilation);
			}

			return default;
		}

		private Project? GetContainingProject(TypeNode typeNode)
		{
			if (_solution == null)
			{
				return null;
			}

			foreach (var file in typeNode.AssociatedFiles)
			{
				var docIds = _solution.GetDocumentIdsWithFilePath(file);
				foreach (var docId in docIds)
				{
					var project = _solution.GetDocument(docId)?.Project;
					if (project != null)
					{
						return project;
					}
				}
			}

			return null;
		}

	}
}
