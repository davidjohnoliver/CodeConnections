#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Roslyn
{
	/// <summary>
	/// A lightweight identifier for a type that considers only the fully-qualified name.
	/// </summary>
	public struct TypeIdentifier
	{
		public string FullName { get; }

		public string Name { get; }

		public TypeIdentifier(string fullName, string name)
		{
			FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		public override bool Equals(object obj) => obj is TypeIdentifier other && FullName == other.FullName ;

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 13;
				hash = hash * 31 + FullName.GetHashCode();
				return hash;
			}
		}

		public override string ToString() => FullName;
	}
}
