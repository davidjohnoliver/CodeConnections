#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Utilities;
using Microsoft.CodeAnalysis;

namespace DependsOnThat.Extensions
{
	public static class SolutionExtensions
	{
		/// <summary>
		/// Get <see cref="Document"/> corresponding to supplied file path.
		/// </summary>
		public static Document? GetDocument(this Solution solution, string filePath)
			=> solution.GetDocument(solution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault());

		public static IEnumerable<DocumentId> GetAllDocumentIds(this Solution solution) => solution.Projects.SelectMany(p => p.DocumentIds);

		/// <summary>
		/// Get symbols of all types declared within the file at the supplied <paramref name="filePath"/>.
		/// </summary>
		public static async Task<IEnumerable<ITypeSymbol>> GetDeclaredSymbolsFromFilePath(this Solution solution, string filePath, CancellationToken ct)
		{
			var document = solution.GetDocument(filePath);
			if (document == null)
			{
				return ArrayUtils.GetEmpty<ITypeSymbol>();
			}
			var syntaxRoot = await document.GetSyntaxRootAsync(ct);
			var semanticModel = await document.GetSemanticModelAsync(ct);
			if (syntaxRoot == null || semanticModel == null)
			{
				return ArrayUtils.GetEmpty<ITypeSymbol>();
			}

			var declaredSymbols = syntaxRoot.GetAllDeclaredTypes(semanticModel);
			return declaredSymbols;
		}
	}
}
