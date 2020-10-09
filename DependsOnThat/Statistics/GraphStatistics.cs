#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using DependsOnThat.Graph;
using DependsOnThat.Graph.Display;
using DependsOnThat.Presentation;

namespace DependsOnThat.Statistics
{
	/// <summary>
	/// Calculates and holds various statistical characteristics of a <see cref="NodeGraph"/>
	/// </summary>
	public class GraphStatistics
	{
		/// <summary>
		/// Statistics about size of mutual dependency clusters.
		/// </summary>
		public DiscreteStatisticsResult<CyclicCluster> ClusterSizeStatistics { get; }

		/// <summary>
		/// Statistics about <see cref="CyclicCluster.SortLayer"/> value of mutual dependency clusters.
		/// </summary>
		public DiscreteStatisticsResult<CyclicCluster> ClusterSortLayerStatistics { get; }

		/// <summary>
		/// Statistics about <see cref="CyclicCluster.SortLayerFromTop"/> value of mutual dependency clusters.
		/// </summary>
		public DiscreteStatisticsResult<CyclicCluster> ClusterSortLayerFromTopStatistics { get; }

		/// <summary>
		/// Statistics about number of dependencies per node.
		/// </summary>
		public DiscreteStatisticsResult<DisplayNodeAndEdges> NodeDependenciesStatistics { get; }

		/// <summary>
		/// Statistics about number of dependents per node.
		/// </summary>
		public DiscreteStatisticsResult<DisplayNodeAndEdges> NodeDependentsStatistics { get; }

		/// <summary>
		/// Create a new set of statistics.
		/// </summary>
		/// <remarks>This may be costly, consider creating on a background thread!</remarks>
		public GraphStatistics(NodeGraph nodeGraph)
		{
			var (clusterGraph, simpleGraph) = nodeGraph.GetFullClusteredGraph();

			ClusterSizeStatistics = DiscreteStatisticsResult.Create(clusterGraph.Vertices, c => c.VertexCount);
			ClusterSortLayerStatistics = DiscreteStatisticsResult.Create(clusterGraph.Vertices, c => c.SortLayer);
			ClusterSortLayerFromTopStatistics = DiscreteStatisticsResult.Create(clusterGraph.Vertices, c => c.SortLayerFromTop);

			NodeDependenciesStatistics = DiscreteStatisticsResult.Create(simpleGraph.Vertices.Select(v => new DisplayNodeAndEdges(v, simpleGraph)), v => simpleGraph.OutDegree(v.DisplayNode));
			NodeDependentsStatistics = DiscreteStatisticsResult.Create(simpleGraph.Vertices.Select(v => new DisplayNodeAndEdges(v, simpleGraph)), v => simpleGraph.InDegree(v.DisplayNode));
		}
	}
}
