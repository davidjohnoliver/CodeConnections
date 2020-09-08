#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace DependsOnThat.Graph
{
	public class TypeNodeKey : NodeKey
	{
		private readonly ITypeSymbol _typeSymbol;

		public TypeNodeKey(ITypeSymbol typeSymbol)
		{
			_typeSymbol = typeSymbol ?? throw new ArgumentNullException(nameof(typeSymbol));
		}

		public override bool Equals(object obj) =>
			// Use default comparer because we don't want to take nullability into account
			obj is TypeNodeKey other && SymbolEqualityComparer.Default.Equals(_typeSymbol, other._typeSymbol);

		public override int GetHashCode() => _typeSymbol.GetHashCode();
	}
}
