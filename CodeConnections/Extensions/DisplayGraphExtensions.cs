#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Graph.Display;
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Algorithms.Condensation;

namespace CodeConnections.Extensions
{
	public static class DisplayGraphExtensions
	{
		/// <summary>
		/// Condenses mutual dependency cycles in a graph of <see cref="DisplayNode"/>s, and calculates properties such as sort layer for each vertex in the condensed graph.
		/// </summary>
		/// <returns>The condensed graph of <see cref="CyclicCluster"/>s.</returns>
		public static IMutableBidirectionalGraph<CyclicCluster, CondensedEdge<DisplayNode, DisplayEdge, CyclicCluster>> GetClusteredGraph(this IBidirectionalGraph<DisplayNode, DisplayEdge> displayGraph)
		{
			var clusterGraph = displayGraph.CondensateStronglyConnected<DisplayNode, DisplayEdge, CyclicCluster>();

			// Set sort layers
			var topoSorted = new List<CyclicCluster>();
			clusterGraph.TopologicalSort(topoSorted);
			var maxSortLayer = 0;
			foreach (var cluster in topoSorted)
			{
				cluster.SortLayer = GetSortLayer(cluster);
				maxSortLayer = Math.Max(maxSortLayer, cluster.SortLayer);
			}

			for (int i= topoSorted.Count-1;i>=0;i--)
			{
				topoSorted[i].SortLayerFromTop = GetSortLayerFromTop(topoSorted[i]);
			}

			var tempSorted = topoSorted.OrderByDescending(cc => cc.SortLayer).ToList();
			;

			return clusterGraph;

			int GetSortLayer(CyclicCluster cluster)
			{
				if (clusterGraph.TryGetInEdges(cluster, out var edges) && edges.Any())
				{
					return edges.Select(e => e.Source.SortLayer).Max() + 1;
				}

				return 0;
			}

			int GetSortLayerFromTop(CyclicCluster cluster)
			{
				if (clusterGraph.TryGetOutEdges(cluster, out var edges) && edges.Any())
				{
					return edges.Select(e => e.Target.SortLayer).Min() - 1;
				}

				return maxSortLayer;
			}
		}

	}
}
