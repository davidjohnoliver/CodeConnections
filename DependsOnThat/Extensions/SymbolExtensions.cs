#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace DependsOnThat.Extensions
{
	public static class SymbolExtensions
	{
		/// <summary>
		/// Returns all types that this symbol's declaration depends upon, by examining the supplied compilation.
		/// </summary>
		/// <param name="compilation">The <see cref="Compilation"/> containing this symbol</param>
		/// 
		/// <param name="includeExternalMetadata">
		/// If true, externally-defined symbols from metadata are included. If false, only locally-defined 
		/// symbols in the same solution are returned.
		/// </param>
		public static async IAsyncEnumerable<ITypeSymbol> GetTypeDependencies(this ISymbol symbol, Compilation compilation, bool includeExternalMetadata = true, [EnumeratorCancellation] CancellationToken ct = default)
		{
			foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
			{
				if (ct.IsCancellationRequested)
				{
					yield break;
				}
				var root = await syntaxRef.GetSyntaxAsync(ct);
				var model = compilation.GetSemanticModel(syntaxRef.SyntaxTree);
				foreach (var referenced in root.GetAllReferencedTypeSymbols(model, includeExternalMetadata))
				{
					yield return referenced;
				}
			}
		}

		/// <summary>
		/// Get the <see cref="Compilation"/> containing the supplied symbol.
		/// </summary>
		/// <param name="containingSolution">The <see cref="Solution"/> in which the symbol is defined.</param>
		public static async Task<Compilation?> GetCompilation(this ISymbol symbol, Solution containingSolution, CancellationToken ct = default)
		{
			var project = containingSolution.GetProject(symbol.ContainingAssembly);

			if (project == null)
			{
				return null;
			}

			return await project.GetCompilationAsync(ct);
		}
	}
}
