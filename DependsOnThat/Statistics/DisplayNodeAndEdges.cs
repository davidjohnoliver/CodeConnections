#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Presentation;
using QuickGraph;

namespace DependsOnThat.Statistics
{
	/// <summary>
	/// Wrapper for <see cref="DisplayNode"/> which permits its edges to be retrieved from containing graph.
	/// </summary>
	public sealed class DisplayNodeAndEdges
	{
		private readonly IBidirectionalGraph<DisplayNode, DisplayEdge> _owner;
		public DisplayNode DisplayNode { get; }

		public IEnumerable<DisplayEdge> InEdges => _owner.InEdges(DisplayNode);

		public IEnumerable<DisplayEdge> OutEdges => _owner.OutEdges(DisplayNode);

		public DisplayNodeAndEdges(DisplayNode displayNode, IBidirectionalGraph<DisplayNode, DisplayEdge> owner)
		{
			DisplayNode = displayNode;
			_owner = owner;
		}
	}
}
