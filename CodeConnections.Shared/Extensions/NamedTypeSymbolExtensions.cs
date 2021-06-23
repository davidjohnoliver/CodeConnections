#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace CodeConnections.Extensions
{
	public static class NamedTypeSymbolExtensions
	{
		/// <summary>
		/// Returns the value of <see cref="INamedTypeSymbol.ConstructedFrom"/> for <paramref name="namedTypeSymbol"/> if it's different from 
		/// <paramref name="namedTypeSymbol"/>; otherwise, returns null.
		/// </summary>
		public static ITypeSymbol? ConstructedFromOrDefault(this INamedTypeSymbol namedTypeSymbol)
		{
			if (!SymbolEqualityComparer.Default.Equals(namedTypeSymbol, namedTypeSymbol.ConstructedFrom))
			{
				return namedTypeSymbol.ConstructedFrom;
			}

			return null;
		}
	}
}
