#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Roslyn;
using Microsoft.CodeAnalysis;

namespace DependsOnThat.Extensions
{
	public static class TypeSymbolExtensions
	{

		/// <summary>
		/// Creates a <see cref="TypeIdentifier"/> from an <see cref="ITypeSymbol"/> that depends only on the full name, suitable for comparisons 
		/// from different compilations, etc.
		/// </summary>
		public static TypeIdentifier ToIdentifier(this ITypeSymbol symbol)
		{
			var fullName = symbol.ToDisplayString();
			// We do it this way instead of using symbol.Name because it includes the type parameters of generic types (and symbol.MetadataName isn't very human-readable)
			var shortName = fullName.Split('.').Last();
			return new TypeIdentifier(fullName, shortName);
		}

		/// <summary>
		/// Returns all the types implied as dependencies by the presence of <paramref name="typeSymbol"/>, ie the various types that went into <paramref name="typeSymbol"/>'s construction.
		/// </summary>
		/// <param name="includeThis">Return <paramref name="typeSymbol"/> itself?</param>
		/// <param name="includeConstructed">
		/// Should 'constructed' types be included? This covers implicit ValueTuple types and not-fully-open generic types (ie generic types with 
		/// concrete or partially-concrete type arguments). This is only applicable to <paramref name="typeSymbol"/> itself, and takes precedence 
		/// over <paramref name="includeThis"/>.
		/// </param>
		public static IEnumerable<ITypeSymbol> Unpack(this ITypeSymbol typeSymbol, bool includeThis, bool includeConstructed)
		{
			var constructedFrom = (typeSymbol as INamedTypeSymbol)?.ConstructedFromOrDefault();
			var isConstructed = constructedFrom != null;
			if (includeThis && (includeConstructed || !isConstructed))
			{
				yield return typeSymbol;
			}

			switch (typeSymbol)
			{
				case IArrayTypeSymbol arrayTypeSymbol:
					foreach (var inner in Unpack(arrayTypeSymbol.ElementType, includeThis: true, includeConstructed))
					{
						yield return inner;
					}
					break;
				case INamedTypeSymbol namedTypeSymbol:
					foreach (var typeArg in namedTypeSymbol.TypeArguments)
					{
						foreach (var inner in Unpack(typeArg, includeThis: true, includeConstructed))
						{
							yield return inner;
						}
					}
					if (constructedFrom != null)
					{
						foreach (var inner in Unpack(constructedFrom, includeThis: true, includeConstructed))
						{
							yield return inner;
						}
					}
					break;
			}
		}

		/// <summary>
		/// <see cref="Unpack(ITypeSymbol, bool, bool)"/> each of a sequence of types.
		/// </summary>
		/// <param name="includeConstructed">See <see cref="Unpack(ITypeSymbol, bool, bool)"/>.</param>
		public static IEnumerable<ITypeSymbol> Unpack(this IEnumerable<ITypeSymbol> typeSymbols, bool includeConstructed)
			=> typeSymbols.SelectMany(t => Unpack(t, includeThis: true, includeConstructed));
	}
}
