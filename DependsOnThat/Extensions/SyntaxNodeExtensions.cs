#nullable enable

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Extensions
{
	public static class SyntaxNodeExtensions
	{
		/// <summary>
		/// Retrieves all symbols referenced in the syntax subtree rooted on <paramref name="syntaxNode"/>.
		/// </summary>
		/// <param name="includeExternalMetadata">
		/// If true, externally-defined symbols from metadata are included. If false, only locally-defined 
		/// symbols in the same compilation are returned.
		/// </param>
		public static IEnumerable<ISymbol> GetAllReferencedSymbols(this SyntaxNode syntaxNode, SemanticModel model, bool includeExternalMetadata = true)
			=> syntaxNode.DescendantNodesAndSelf()
				.Select(n => model.GetSymbolInfo(n).Symbol)
				.Trim()
				.Where(s => includeExternalMetadata ? true : !s.DeclaringSyntaxReferences.IsEmpty)
				.Distinct();

		/// <summary>
		/// Retrieves all <see cref="ITypeSymbol"/> symbols referenced in the syntax subtree rooted on <paramref name="syntaxNode"/>.
		/// </summary>
		/// <param name="includeExternalMetadata">
		/// If true, externally-defined symbols from metadata are included. If false, only locally-defined 
		/// symbols in the same compilation are returned.
		/// </param>
		public static IEnumerable<ITypeSymbol> GetAllReferencedTypeSymbols(this SyntaxNode syntaxNode, SemanticModel model, bool includeExternalMetadata = true, bool includeTypeParameters = false)
			=> GetAllReferencedSymbols(syntaxNode, model, includeExternalMetadata).OfType<ITypeSymbol>()
				.Where(s => includeTypeParameters ? true : !(s is ITypeParameterSymbol));

	}
}
