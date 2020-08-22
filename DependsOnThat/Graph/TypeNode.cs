#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace DependsOnThat.Graph
{
	/// <summary>
	/// A node that is associated with a particular defined type.
	/// </summary>
	public class TypeNode : Node
	{
		public ITypeSymbol Symbol { get; }

		public TypeNode(ITypeSymbol typeSymbol)
		{
			Symbol = typeSymbol;
		}

		public override string ToString()
		{
			return $"{base.ToString()}-{Symbol?.ToString()}";
		}
	}
}
