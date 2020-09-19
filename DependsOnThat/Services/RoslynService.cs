﻿#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using DependsOnThat.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;

namespace DependsOnThat.Services
{
	internal class RoslynService : IRoslynService
	{
		private readonly VisualStudioWorkspace _workspace;

		public RoslynService(VisualStudioWorkspace workspace)
		{
			_workspace = workspace;
		}

		public async IAsyncEnumerable<(string FilePath, ITypeSymbol Symbol)> GetDeclaredSymbolsFromFilePaths(IEnumerable<string> filePaths, [EnumeratorCancellation] CancellationToken ct)
		{
			var solution = GetCurrentSolution();
			foreach (var filePath in filePaths)
			{
				var document = solution.GetDocument(filePath);
				if (document == null)
				{
					continue;
				}
				var syntaxRoot = await document.GetSyntaxRootAsync(ct);
				var semanticModel = await document.GetSemanticModelAsync(ct);
				if (syntaxRoot == null || semanticModel == null)
				{
					continue;
				}

				var declaredSymbols = syntaxRoot.GetAllDeclaredTypes(semanticModel);
				foreach (var symbol in declaredSymbols)
				{
					yield return (filePath, symbol);
				}
			}
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
	}
}
