#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Graph
{
	/// <summary>
	/// Defines a key which can be used to identify or retrieve a given <see cref="Node"/>.
	/// </summary>
	public abstract class NodeKey
	{
		public override abstract bool Equals(object? obj);

		public override abstract int GetHashCode();

		public static bool operator ==(NodeKey? key1, NodeKey? key2) => key1?.Equals(key2) ?? key2 is null;
		public static bool operator !=(NodeKey? key1, NodeKey? key2) => !(key1 == key2);
	}
}
