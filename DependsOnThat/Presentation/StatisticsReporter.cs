#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using DependsOnThat.Statistics;
using DependsOnThat.Text;
using static DependsOnThat.Presentation.StatisticsReportContent;

namespace DependsOnThat.Presentation
{
	/// <summary>
	/// Generates formatted output from a set of <see cref="GraphStatistics"/>.
	/// </summary>
	public class StatisticsReporter
	{
		private readonly GraphStatistics _graphStatistics;
		private readonly IHeaderFormatter _headerFormatter;
		private readonly IDictionaryFormatter _dictionaryFormatter;
		private readonly ICompactListFormatter _listFormatter1;
		private readonly ICompactListFormatter _listFormatter2;
		private readonly (int AtLeast, int NoMoreThan) _showTopXDeps;
		private readonly (int AtLeast, int NoMoreThan) _showTopXClusters;

		public StatisticsReporter(GraphStatistics graphStatistics, IHeaderFormatter headerFormatter, IDictionaryFormatter dictionaryFormatter, ICompactListFormatter listFormatter1, ICompactListFormatter listFormatter2, (int AtLeast, int NoMoreThan) showTopXDeps, (int AtLeast, int NoMoreThan) showTopXClusters)
		{
			_graphStatistics = graphStatistics ?? throw new ArgumentNullException(nameof(graphStatistics));
			_headerFormatter = headerFormatter ?? throw new ArgumentNullException(nameof(headerFormatter));
			_dictionaryFormatter = dictionaryFormatter ?? throw new ArgumentNullException(nameof(dictionaryFormatter));
			_listFormatter1 = listFormatter1 ?? throw new ArgumentNullException(nameof(listFormatter1));
			_listFormatter2 = listFormatter2 ?? throw new ArgumentNullException(nameof(listFormatter2));
			_showTopXDeps = showTopXDeps;
			_showTopXClusters = showTopXClusters;
		}

		public IEnumerable<string> WriteStatistics(StatisticsReportContent statisticsReportContent)
		{
			if (statisticsReportContent.HasFlag(General))
			{
				yield return _headerFormatter.FormatHeader("DependsOnThat statistics", headerLevel: 1);

				yield return _headerFormatter.FormatHeader("Types by dependencies and dependents", headerLevel: 2);

				yield return _headerFormatter.FormatHeader("Types by dependents", headerLevel: 3);
				yield return $"Types with the most dependents: {_listFormatter1.FormatList(GetMostDependents())}";
				yield return $"Mean dependents per type: {_graphStatistics.NodeDependentsStatistics.Mean}";

				foreach (var row in _dictionaryFormatter.FormatDictionary("Dependents", "# of types", _graphStatistics.NodeDependentsStatistics.SparseHistogram))
				{
					yield return row;
				}

				yield return _headerFormatter.FormatHeader("Types by dependencies", headerLevel: 3);
				yield return $"Types with the most dependencies: {_listFormatter1.FormatList(GetMostDependencies())}";
				yield return $"Mean dependencies per type: {_graphStatistics.NodeDependenciesStatistics.Mean}";

				foreach (var row in _dictionaryFormatter.FormatDictionary("Dependencies", "# of types", _graphStatistics.NodeDependenciesStatistics.SparseHistogram))
				{
					yield return row;
				}


				yield return _headerFormatter.FormatHeader("Mutual dependency cycles", headerLevel: 2);
				foreach (var line in GetTopClusters())
				{
					yield return line;
				}
				foreach (var row in _dictionaryFormatter.FormatDictionary("Cycle size", "# of cycles", _graphStatistics.ClusterSizeStatistics.SparseHistogram))
				{
					yield return row;
				}
			}

			IEnumerable<string> GetMostDependencies()
			{
				var mostDependencies = _graphStatistics.NodeDependenciesStatistics.GetTopItems(_showTopXDeps.AtLeast, _showTopXDeps.NoMoreThan, minSampleValue: 1);
				return mostDependencies.Select(dnae => $"{dnae.DisplayNode.DisplayString} ({dnae.OutEdges.Count()})");
			}

			IEnumerable<string> GetMostDependents()
			{
				var mostDependents = _graphStatistics.NodeDependentsStatistics.GetTopItems(_showTopXDeps.AtLeast, _showTopXDeps.NoMoreThan, minSampleValue: 1);
				return mostDependents.Select(dnae => $"{dnae.DisplayNode.DisplayString} ({dnae.InEdges.Count()})");
			}

			IEnumerable<string> GetTopClusters()
			{
				var topClusters = _graphStatistics.ClusterSizeStatistics.GetTopItems(_showTopXClusters.AtLeast, _showTopXClusters.NoMoreThan, minSampleValue: 2);
				if (topClusters.None())
				{
					yield return "No cycles found.";
					yield break;
				}

				yield return "Largest cycles:";

				foreach (var cluster in topClusters)
				{
					yield return $"{cluster.VertexCount} types: {_listFormatter2.FormatList(cluster.Vertices.Select(n => n.DisplayString))}";
				}
			}

			if (statisticsReportContent.HasFlag(GraphingSpecific))
			{
				yield return _headerFormatter.FormatHeader("Detailed graphing-related statistics", headerLevel: 2);

				yield return _headerFormatter.FormatHeader("SortLayer statistics", headerLevel: 3);
				yield return $"Widest SortLayer: {_graphStatistics.ClusterSortLayerStatistics.MaxBucketCountBucket}, with {_graphStatistics.ClusterSortLayerStatistics.MaxBucketCount} items";
				yield return $"Mean SortLayer width: {_graphStatistics.ClusterSortLayerStatistics.MeanBucketCount}";
				yield return $"SortLayer width S.D.: {_graphStatistics.ClusterSortLayerStatistics.SDBucketCount}";
				foreach (var row in _dictionaryFormatter.FormatDictionary("SortLayer", "Types in layer", _graphStatistics.ClusterSortLayerStatistics.Histogram))
				{
					yield return row;
				}

				yield return _headerFormatter.FormatHeader("SortLayerFromTop statistics", headerLevel: 3);
				yield return $"Widest SortLayerFromTop: {_graphStatistics.ClusterSortLayerFromTopStatistics.MaxBucketCountBucket}, with {_graphStatistics.ClusterSortLayerFromTopStatistics.MaxBucketCount} items";
				yield return $"Mean SortLayerFromTop width: {_graphStatistics.ClusterSortLayerFromTopStatistics.MeanBucketCount}";
				yield return $"SortLayerFromTop width S.D.: {_graphStatistics.ClusterSortLayerFromTopStatistics.SDBucketCount}";
				foreach (var row in _dictionaryFormatter.FormatDictionary("SortLayerFromTop", "Types in layer", _graphStatistics.ClusterSortLayerFromTopStatistics.Histogram))
				{
					yield return row;
				}
			}
		}
	}
}
