﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeConnections.Disposables;
using CodeConnections.Extensions;
using CodeConnections.Graph;
using CodeConnections.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Threading;

namespace CodeConnections.Roslyn
{
	/// <summary>
	/// Caches <see cref="SemanticModel"/>s and other Roslyn compilation data for reuse within a specific time window.
	/// </summary>
	/// <remarks>
	/// It's assumed that <see cref="SetSolution(Solution)"/> and <see cref="ClearSolution"/> will only ever be called from the main thread,
	/// but read methods may be called from any thread.
	/// </remarks>
	public sealed class CompilationCache
	{
		private readonly object _gate = new object();
		private bool _isActive = false;

		private Solution? _solution;
		private readonly Dictionary<Project, Compilation?> _cachedCompilations = new Dictionary<Project, Compilation?>();
		private readonly Dictionary<SyntaxTree, SemanticModel?> _cachedSemanticModels = new Dictionary<SyntaxTree, SemanticModel?>();
		private readonly Dictionary<TypeNode, (INamedTypeSymbol?, ProjectIdentifier)> _cachedTypeSymbols = new();
		private CancellationDisposable? _cancellationDisposable;

		/// <summary>
		/// Activates the cache using <paramref name="solution"/>.
		/// </summary>
		public void SetSolution(Solution solution)
		{
			if (solution is null)
			{
				throw new ArgumentNullException(nameof(solution));
			}

			if (_solution is { })
			{
				if (_solution == solution)
				{
					// No need to do anything
					return;
				}
				else
				{
					ClearSolution();
				}
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
		public void ClearSolution()
		{
			IDisposable? cd = null;
			lock (_gate)
			{
				_isActive = false;
				_solution = null;
				_cachedCompilations.Clear();
				_cachedSemanticModels.Clear();
				_cachedTypeSymbols.Clear();
				cd = _cancellationDisposable;
				_cancellationDisposable = null;
			}

			cd?.Dispose();
		}

		/// <summary>
		/// Gets the current solution.
		/// </summary>
		/// <returns>A <see cref="CancellationToken"/> that will be cancelled if the solution changes or <paramref name="ct"/> is cancelled.</returns>
		private (Solution?, CancellationToken ct) GetSolution(CancellationToken ct)
		{
			Solution? solution;

			lock (_gate)
			{
				if (!_isActive)
				{
					return (null, ct);
				}
				if (_solution == null)
				{
					throw new InvalidOperationException();
				}
				solution = _solution;

				ct = GetCombined(ct);
			}

			return (solution, ct);
		}

		private Solution? GetSolution()
		{
			var (solution, _) = GetSolution(default);
			return solution;
		}

		private (Project?, CancellationToken) GetProject(ProjectIdentifier projectIdentifier, CancellationToken ct)
		{
			Solution? solution;
			(solution, ct) = GetSolution(ct);
			var project = solution?.GetProject(projectIdentifier.Id);
			return (project, ct);
		}

		private Project? GetProject(ProjectIdentifier projectIdentifier)
		{
			var (project, _) = GetProject(projectIdentifier, default);
			return project;
		}

		private async Task<Compilation?> GetCompilation(ProjectIdentifier projectIdentifier, CancellationToken ct)
		{
			Project? project;
			(project, ct) = GetProject(projectIdentifier, ct);

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

		public async Task<SemanticModel?> GetSemanticModel(SyntaxTree syntaxTree, ProjectIdentifier projectIdentifier, CancellationToken ct)
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
		/// Gets the type symbol corresponding to <paramref name="node"/>, as well as the project it belongs to. Returns null for both 
		/// if cache is inactive, and null for either if not found.
		/// </summary>
		public async Task<(INamedTypeSymbol? Symbol, ProjectIdentifier ProjectIdentifier)> GetSymbolForNode(Node node, CancellationToken ct)
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

				var projectIdentifier = project.ToIdentifier();
				var compilation = await GetCompilation(projectIdentifier, ct);

				if (compilation == null)
				{
					return default;
				}

				var symbol = compilation.GetTypeByMetadataName(typeNode.FullMetadataName);

				lock (_gate)
				{
					if (_isActive)
					{
						_cachedTypeSymbols[typeNode] = (symbol, projectIdentifier);
					}
				}

				return (symbol, projectIdentifier);
			}

			return default;
		}

		/// <summary>
		/// Get all syntax trees belonging to <paramref name="projectIdentifier"/>.
		/// </summary>
		public async Task<IEnumerable<SyntaxTree>> GetSyntaxTreesForProject(ProjectIdentifier projectIdentifier, CancellationToken ct)
		{
			var compilation = await GetCompilation(projectIdentifier, ct);

			if (compilation == null)
			{
				return ArrayUtils.GetEmpty<SyntaxTree>();
			}

			return compilation.SyntaxTrees;
		}

		/// <summary>
		/// Get symbols of all types declared within the file at the supplied <paramref name="filePath"/>.
		/// </summary>
		public async Task<IEnumerable<ITypeSymbol>> GetDeclaredSymbolsFromFilePath(string filePath, CancellationToken ct)
		{
			Solution? solution;
			(solution, ct) = GetSolution(ct);
			var id = solution?.GetDocumentIdsWithFilePath(filePath).FirstOrDefault();
			var document = solution?.GetDocument(id);

			if (document == null)
			{
				return Default();
			}

			var syntaxRoot = await document.GetSyntaxRootAsync(ct);
			if (ct.IsCancellationRequested)
			{
				return Default();
			}

			if (syntaxRoot == null)
			{
				return Default();
			}

			var semanticModel = await GetSemanticModel(syntaxRoot, document.Project.ToIdentifier(), ct);

			if (syntaxRoot == null || semanticModel == null)
			{
				return Default();
			}

			var declaredSymbols = syntaxRoot.GetAllDeclaredTypes(semanticModel);
			return declaredSymbols;

			ITypeSymbol[] Default() => ArrayUtils.GetEmpty<ITypeSymbol>();
		}

		public Document? GetDocument(DocumentId? documentId)
		{
			if (!_isActive)
			{
				return null;
			}

			return GetSolution()?.GetDocument(documentId);
		}

		private Project? GetContainingProject(TypeNode typeNode) => GetContainingProject(typeNode.AssociatedFiles);

		public Project? GetContainingProject(IEnumerable<string> associatedFiles)
		{
			var solution = GetSolution();
			if (solution == null)
			{
				return null;
			}

			foreach (var file in associatedFiles)
			{
				var docIds = solution.GetDocumentIdsWithFilePath(file);
				foreach (var docId in docIds)
				{
					// Although we're two loops deep here, typically the first associated file will belong to a project, and it's not immediately
					// clear why there would ever be multiple DocumentIds for a single file path
					var project = solution.GetDocument(docId)?.Project;
					if (project != null)
					{
						return project;
					}
				}
			}

			return null;
		}

		public IEnumerable<ProjectIdentifier> GetAllProjects()
		{
			var solution = GetSolution();
			return solution?.Projects.Select(p => p.ToIdentifier()) ?? ArrayUtils.GetEmpty<ProjectIdentifier>();
		}

		public string? GetAssemblyName(ProjectIdentifier projectIdentifier)
		{
			var project = GetProject(projectIdentifier);

			return project?.AssemblyName;
		}

		/// <summary>
		/// Returns a cache pre-loaded with <paramref name="solution"/>, used by tests.
		/// </summary>
		public static CompilationCache CacheWithSolution(Solution solution)
		{
			var cache = new CompilationCache();
			cache.SetSolution(solution);
			return cache;
		}
	}
}
