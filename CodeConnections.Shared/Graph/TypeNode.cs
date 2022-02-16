#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Extensions;
using CodeConnections.Roslyn;
using Microsoft.CodeAnalysis;

namespace CodeConnections.Graph
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

		public bool IsNestedType { get; }

		public ProjectIdentifier? Project { get; }

		/// <summary>
		/// Lines of code associated with the type, across all partial definitions.
		/// </summary>
		/// <remarks>
		/// This is mutable, since it may be updated after the graph has been built.
		/// </remarks>
		public int LineCount { get; set; }

		public TypeNode(TypeNodeKey key, string? filePath, IEnumerable<string> associatedFiles, string fullMetadataName, int lineCount, bool isNestedType, ProjectIdentifier? project)
		{
			Identifier = key.Identifier;
			FilePath = filePath;
			FullMetadataName = fullMetadataName;
			LineCount = lineCount;
			IsNestedType = isNestedType;
			Project = project;
			AssociatedFiles.AddRange(associatedFiles);
			Key = key;
		}

		public override string ToString() => $"{base.ToString()}-{Identifier.ToString()}";
	}
}
