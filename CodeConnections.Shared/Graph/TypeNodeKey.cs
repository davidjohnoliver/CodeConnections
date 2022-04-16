#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Roslyn;
using Microsoft.CodeAnalysis;

namespace CodeConnections.Graph
{
	public class TypeNodeKey : NodeKey
	{
		public TypeIdentifier Identifier { get; }

		public TypeNodeKey(TypeIdentifier identifier)
		{
			Identifier = identifier;
		}

		public override bool Equals(object? obj) => obj is TypeNodeKey other && Equals(Identifier, other.Identifier);

		public override int GetHashCode() => Identifier.GetHashCode();

		public static TypeNodeKey GetFromFullName(string fullName)
		{
			var shortName = fullName.Split('.').Last();
			return new TypeNodeKey(new TypeIdentifier(fullName, shortName));
		}

		public override string ToString() => $"{Identifier}(TypeNodeKey)";
	}
}
