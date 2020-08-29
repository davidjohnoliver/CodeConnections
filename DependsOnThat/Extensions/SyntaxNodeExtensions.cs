#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
		/// symbols in the same solution are returned.
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
		/// symbols in the same solution are returned.
		/// </param>
		public static IEnumerable<ITypeSymbol> GetAllReferencedTypeSymbols(this SyntaxNode syntaxNode, SemanticModel model, bool includeExternalMetadata = true, bool includeTypeParameters = false)
			=> GetAllReferencedSymbols(syntaxNode, model, includeExternalMetadata)
				.Select(s => s as ITypeSymbol
					// Extension method invocations typically otherwise yield no explicit reference to the type
					?? (s as IMethodSymbol)?.ContainingType)
				.Trim()
				.Distinct()
				.Where(s => includeTypeParameters ? true : !(s is ITypeParameterSymbol));

		/// <summary>
		/// Get all types (classes/structs, interfaces, and enums) declared by or within <paramref name="syntaxNode"/>.
		/// </summary>
		/// <returns>Symbols of declared types.</returns>
		public static IEnumerable<ITypeSymbol> GetAllDeclaredTypes(this SyntaxNode syntaxNode, SemanticModel model) => syntaxNode.DescendantNodesAndSelf()
			.OfType<BaseTypeDeclarationSyntax>()
			.Select(n => model.GetDeclaredSymbol(n) as ITypeSymbol)
			.Trim();
	}
}
