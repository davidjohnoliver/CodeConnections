#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Roslyn;
using Microsoft.CodeAnalysis;

namespace DependsOnThat.Graph
{
	public class TypeNodeKey : NodeKey
	{
		private readonly TypeIdentifier _identifier;

		public TypeNodeKey(TypeIdentifier identifier)
		{
			_identifier = identifier;
		}

		public override bool Equals(object obj) => obj is TypeNodeKey other && Equals(_identifier, other._identifier);

		public override int GetHashCode() => _identifier.GetHashCode();

		public static TypeNodeKey GetFromFullName(string fullName)
		{
			var shortName = fullName.Split('.').Last();
			return new TypeNodeKey(new TypeIdentifier(fullName, shortName));
		}
	}
}
