#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace CodeConnections.Roslyn
{
	/// <summary>
	/// A lightweight identifier for a <see cref="Project"/>.
	/// </summary>
	public struct ProjectIdentifier
	{
		public ProjectIdentifier(ProjectId id, string projectName)
		{
			Id = id;
			ProjectName = projectName;
		}

		public ProjectId Id { get; }
		public string ProjectName { get; }

		public override bool Equals(object? obj) => obj is ProjectIdentifier other && other.Id == Id;

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 13;
				hash = hash * 31 + Id.GetHashCode();
				return hash;
			}
		}

		public override string ToString() => $"{base.ToString()}-'{ProjectName}'";
	}
}
