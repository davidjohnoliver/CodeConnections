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
		public override NodeKey Key { get; }

		public ITypeSymbol Symbol { get; }

		public string? FilePath { get; }

		public TypeNode(ITypeSymbol typeSymbol, string? filePath)
		{
			Symbol = typeSymbol;
			FilePath = filePath;

			Key = new TypeNodeKey(typeSymbol);
		}

		public override string ToString() => $"{base.ToString()}-{Symbol?.ToString()}";
	}
}
