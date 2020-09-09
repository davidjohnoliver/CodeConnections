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
	/// <summary>
	/// A node that is associated with a particular defined type.
	/// </summary>
	public class TypeNode : Node
	{
		public override NodeKey Key { get; }

		public TypeIdentifier Identifier { get; }

		public string? FilePath { get; }

		public TypeNode(TypeIdentifier identifier, string? filePath)
		{
			Identifier = identifier;
			FilePath = filePath;

			Key = new TypeNodeKey(identifier);
		}

		public override string ToString() => $"{base.ToString()}-{Identifier.ToString()}";
	}
}
