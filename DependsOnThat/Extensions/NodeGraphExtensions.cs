using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Graph;
using DependsOnThat.Presentation;
using QuickGraph;

namespace DependsOnThat.Extensions
{
	public static class NodeGraphExtensions
	{
		/// <summary>
		/// Get subgraph ready for display.
		/// </summary>
		/// <param name="extensionDepth">
		/// The depth to extend the subgraph from the <see cref="NodeGraph.Roots"/>. A value of 0 will only include the roots, a value of 1 will 
		/// include 1st-nearest neighbours (both upstream and downstream), etc.
		/// </param>
		public static IBidirectionalGraph<DisplayNode, DisplayEdge> GetDisplaySubgraph(this NodeGraph nodeGraph, int extensionDepth)
		{
			var subgraphNodes = nodeGraph.ExtendSubgraphFromRoots(extensionDepth);
			var graph = new BidirectionalGraph<DisplayNode, DisplayEdge>();
			var displayNodes = subgraphNodes.ToDictionary(n => n, n => n.ToDisplayNode());
			foreach (var kvp in displayNodes)
			{
				graph.AddVertex(kvp.Value);
			}

			foreach (var kvp in displayNodes)
			{
				foreach (var link in kvp.Key.ForwardLinks)
				{
					if (displayNodes.ContainsKey(link))
					{
						// Add dependencies as edges if both ends are part of the subgraph
						graph.AddEdge(new DisplayEdge(kvp.Value, displayNodes[link]));
					}
				}
			}

			return graph;
		}
	}
}
