#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using DependsOnThat.Extensions;

namespace DependsOnThat.Tests.Extensions
{
	public static class SolutionExtensions
	{
		public static async IAsyncEnumerable<(ITypeSymbol Symbol, Compilation Compilation)> GetAllDeclaredTypes(this Solution solution)
		{
			foreach (var project in solution.Projects)
			{
				var compilation = await project.GetCompilationAsync() ?? throw new InvalidOperationException();

				foreach (var document in project.Documents)
				{
					var syntaxRoot = await document.GetSyntaxRootAsync() ?? throw new InvalidOperationException();
					var semanticModel = compilation.GetSemanticModel(syntaxRoot.SyntaxTree);
					foreach (var type in syntaxRoot.GetAllDeclaredTypes(semanticModel))
					{
						yield return (type, compilation);
					}
				}
			}
		}
	}
}
