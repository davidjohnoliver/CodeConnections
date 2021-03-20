#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Roslyn;
using Microsoft.CodeAnalysis;

namespace DependsOnThat.Extensions
{
	public static class SymbolExtensions
	{
		/// <summary>
		/// Returns all types that this symbol's declaration depends upon, by examining the supplied compilation.
		/// </summary>
		/// <param name="includeExternalMetadata">
		/// If true, externally-defined symbols from metadata are included. If false, only locally-defined 
		/// symbols in the same solution are returned.
		/// </param>
		public static async IAsyncEnumerable<ITypeSymbol> GetTypeDependencies(this ISymbol symbol, CompilationCache compilationCache, ProjectIdentifier containingProject, bool includeExternalMetadata = true, [EnumeratorCancellation] CancellationToken ct = default)
		{
			foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
			{
				if (ct.IsCancellationRequested)
				{
					yield break;
				}
				var root = await syntaxRef.GetSyntaxAsync(ct);
				var model = await compilationCache.GetSemanticModel(syntaxRef.SyntaxTree, containingProject, ct);
				if (model == null)
				{
					continue;
				}
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

		/// <summary>
		/// Get the 'preferred' declaration from potentially multiple declarations for this symbol
		/// </summary>
		/// <remarks>
		/// The 'preference' is for the filename with fewest suffixes (eg Foo.cs over Foo.suffix.cs), with ties broken by the 
		/// shortest total filepath (dir/Foo.cs over dir/subdir/Foo.cs).
		/// </remarks>
		public static string? GetPreferredDeclaration(this ISymbol symbol)
		{
			if (symbol.DeclaringSyntaxReferences.Length == 1)
			{
				return symbol.DeclaringSyntaxReferences[0].SyntaxTree.FilePath;
			}

			return GetPreferredSymbolDeclaration(symbol.DeclaringSyntaxReferences.Select(sr => sr.SyntaxTree.FilePath));
		}

		public static string? GetPreferredSymbolDeclaration(IEnumerable<string> declarations) => declarations
			.Where(s => !string.IsNullOrWhiteSpace(s))
			.OrderBy(s => Path.GetFileName(s).Split('.').Length)
			.ThenBy(s => s.Length)
			.FirstOrDefault();

		/// <summary>
		/// Returns true if all of <paramref name="symbol"/>'s declarations occur in generated code. Eg, if it is a type, this will return 
		/// true if all partial declarations of the type are in generated code files.
		/// 
		/// If there are no declarations in syntax, returns false.
		/// </summary>
		public static bool IsPurelyGeneratedSymbol(this ISymbol symbol)
			=> symbol.DeclaringSyntaxReferences.Length > 0 && symbol.DeclaringSyntaxReferences.All(tr => tr.SyntaxTree.IsGeneratedCode());

		/// <summary>
		/// Get the fullly-qualified metadata name for <paramref name="typeSymbol"/>.
		/// </summary>
		public static string GetFullMetadataName(this ITypeSymbol typeSymbol)
		{
			// Taken from https://stackoverflow.com/a/27106959/1902058
			ISymbol s = typeSymbol;
			if (s == null || IsRootNamespace(s))
			{
				return string.Empty;
			}

			var sb = new StringBuilder(s.MetadataName);
			var last = s;

			s = s.ContainingSymbol;

			while (!IsRootNamespace(s))
			{
				if (s is ITypeSymbol && last is ITypeSymbol)
				{
					sb.Insert(0, '+');
				}
				else
				{
					sb.Insert(0, '.');
				}

				sb.Insert(0, s.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
				s = s.ContainingSymbol;
			}

			return sb.ToString();
		}

		private static bool IsRootNamespace(ISymbol symbol) => (symbol is INamespaceSymbol s) && s.IsGlobalNamespace;
	}
}
