#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Extensions
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
				.Where(s => includeExternalMetadata ? true : IsDefinedInSolution(s))
				.Distinct();

		private static bool IsDefinedInSolution(ISymbol s)
		{
			return !s.DeclaringSyntaxReferences.IsEmpty;
		}

		/// <summary>
		/// Retrieves all <see cref="ITypeSymbol"/> symbols referenced in the syntax subtree rooted on <paramref name="syntaxNode"/>.
		/// </summary>
		/// <param name="includeExternalMetadata">
		/// If true, externally-defined symbols from metadata are included. If false, only locally-defined 
		/// symbols in the same solution are returned.
		/// </param>
		public static IEnumerable<ITypeSymbol> GetAllReferencedTypeSymbols(this SyntaxNode syntaxNode, SemanticModel model, bool includeExternalMetadata = true, bool includeTypeParameters = false, bool includeConstructed = false)
			=> GetAllReferencedSymbols(syntaxNode, model, includeExternalMetadata: true) // Get all symbols including external ones, to be able to unpack constructed types (generics etc)
				.Select(s => s as ITypeSymbol
					// Extension method invocations typically otherwise yield no explicit reference to the type
					?? (s as IMethodSymbol)?.ContainingType)
				.Trim()
				.Distinct()
				.Unpack(includeConstructed)
				.Trim()
				.Where(s => includeExternalMetadata ? true : IsDefinedInSolution(s)) // Now we filter out external types (if so requested)
				.Where(s => includeTypeParameters ? true : !(s is ITypeParameterSymbol))
				.Distinct();

		/// <summary>
		/// Get all types (classes/structs, interfaces, and enums) declared by or within <paramref name="syntaxNode"/>.
		/// </summary>
		/// <returns>Symbols of declared types.</returns>
		public static IEnumerable<ITypeSymbol> GetAllDeclaredTypes(this SyntaxNode syntaxNode, SemanticModel model) => syntaxNode.DescendantNodesAndSelf()
			// This might well be significantly optimized by not entering into nodes that can be known not to contain declarations
			.OfType<BaseTypeDeclarationSyntax>()
			.Select(n => model.GetDeclaredSymbol(n) as ITypeSymbol)
			.Trim();
	}
}
