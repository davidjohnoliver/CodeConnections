#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
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

		public string FullMetadataName { get; }

		public TypeNode(TypeNodeKey key, string? filePath, IEnumerable<string> associatedFiles, string fullMetadataName)
		{
			Identifier = key.Identifier;
			FilePath = filePath;
			FullMetadataName = fullMetadataName;
			AssociatedFiles.AddRange(associatedFiles);
			Key = key;
		}

		public override string ToString() => $"{base.ToString()}-{Identifier.ToString()}";
	}
}
