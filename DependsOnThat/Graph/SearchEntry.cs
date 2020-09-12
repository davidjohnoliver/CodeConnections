#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Graph
{
	public class SearchEntry
	{
		public readonly Node Node;
		public readonly SearchEntry? Previous;
		public readonly int Generation;

		public SearchEntry(Node node, SearchEntry? previous, int generation)
		{
			Node = node;
			Previous = previous;
			Generation = generation;
		}

		public SearchEntry NextGeneration(Node next) => new SearchEntry(node: next, previous: this, generation: Generation + 1);
	}
}
