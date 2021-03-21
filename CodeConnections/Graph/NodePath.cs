#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Graph
{
	/// <summary>
	/// Describes a path by which <see cref="Target"/> dependency can be reached from <see cref="Source"/>.
	/// </summary>
	public class NodePath : IReadOnlyList<Node>
	{
		public Node Source { get; }

		public Node Target { get; }

		public IReadOnlyList<Node> Intermediates { get; }

		public IEnumerable<Node> All
		{
			get
			{
				yield return Source;
				foreach (var intermediate in Intermediates)
				{
					yield return intermediate;
				}
				yield return Target;
			}
		}

		public int IntermediateLength => Intermediates.Count;

		public int Count => Intermediates.Count + 2;

		public Node this[int index]
		{
			get
			{
				if (index == 0)
				{
					return Source;
				}
				var adjustedIndex = index - 1;
				if (adjustedIndex == Intermediates.Count)
				{
					return Target;
				}
				return Intermediates[adjustedIndex];
			}
		}

		public NodePath(Node source, Node target, IEnumerable<Node> intermediates) : this(source, target, intermediates.ToArray()) { }

		private NodePath(Node source, Node target, Node[] intermediates)
		{
			Source = source;
			Target = target;
			Intermediates = intermediates;
		}

		public IEnumerator<Node> GetEnumerator()
		{
			yield return Source;
			foreach (var node in Intermediates)
			{
				yield return node;
			}
			yield return Target;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		[EditorBrowsable(EditorBrowsableState.Never)]
		public Node[] Slice(int start, int length)
		{
			var slice = new Node[length];
			for (int i = start; i < start + length; i++)
			{
				slice[i - start] = this[i];
			}
			return slice;
		}

		public static NodePath FromSearch(SearchEntry searchEntry, Node target)
		{
			var intermediates = new Node[searchEntry.Generation];
			var current = searchEntry;
			for (int i = searchEntry.Generation - 1; i >= 0; i--)
			{
				intermediates[i] = current?.Node ?? throw new InvalidOperationException();
				current = current.Previous ?? throw new InvalidOperationException();
			}
			Debug.Assert(current.Previous == null);

			return new NodePath(source: current.Node, target, intermediates);
		}
	}
}
