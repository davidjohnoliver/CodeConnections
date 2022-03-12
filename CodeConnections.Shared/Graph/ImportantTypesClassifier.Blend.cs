#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeConnections.Graph
{
	partial class ImportantTypesClassifier
	{
		private class HouseBlendClassifier : ImportantTypesClassifier
		{
			const double BlendScoreBucketFraction = 0.2;

			const double BlendWeightsDependents = 1;
			const double BlendWeightsDependencies = 1;
			const double BlendWeightsLOC = 1;

			private readonly SimpleClassifier _dependentsClassifier;
			private readonly SimpleClassifier _dependenciesClassifier;
			private readonly SimpleClassifier _locClassifier;
			public HouseBlendClassifier()
			{
				_dependentsClassifier = (SimpleClassifier)GetForMode(ImportantTypesMode.MostDependents);
				_dependenciesClassifier = (SimpleClassifier)GetForMode(ImportantTypesMode.MostDependencies);
				_locClassifier = (SimpleClassifier)GetForMode(ImportantTypesMode.MostLOC);
			}

			public override IEnumerable<NodeKey> GetImportantTypes(NodeGraph fullGraph, int noRequested)
			{
				var allScores = fullGraph.Nodes.Select(
					kvp => new NodeScores(kvp.Key)
					{
						DependentsScore = _dependentsClassifier.GetScore(kvp.Value),
						DependenciesScore = _dependenciesClassifier.GetScore(kvp.Value),
						LOCScore = _locClassifier.GetScore(kvp.Value),
					}
				).ToList();

				{
					// Normalize scores by respective averages
					var dependentsTotal = allScores.Sum(s => s.DependentsScore);
					var dependenciesTotal = allScores.Sum(s => s.DependenciesScore);
					var locTotal = allScores.Sum(s => s.LOCScore);

					var dependentsInvAverage = allScores.Count / dependentsTotal;
					var dependenciesInvAverage = allScores.Count / dependenciesTotal;
					var locInvAverage = allScores.Count / locTotal;
					foreach (var score in allScores)
					{
						score.DependentsScore *= dependentsInvAverage;
						score.DependenciesScore *= dependenciesInvAverage;
						score.LOCScore *= locInvAverage;
					}
				}

				noRequested = Math.Min(noRequested, allScores.Count);

				var importantTypes = new HashSet<NodeKey>();
				var firstTarget = noRequested * (1 - BlendScoreBucketFraction);
				var mostDependents = allScores.OrderByDescending(s => s.DependentsScore).GetEnumerator();
				var mostDependencies = allScores.OrderByDescending(s => s.DependenciesScore).GetEnumerator();
				while (importantTypes.Count < firstTarget)
				{
					mostDependents.MoveNext();
					importantTypes.Add(mostDependents.Current.Node);
					mostDependencies.MoveNext();
					importantTypes.Add(mostDependencies.Current.Node);
					// This might add one more than firstTarget, but it's not a big deal
				}

				var byBlendScore = allScores.OrderByDescending(s =>
					s.DependentsScore * BlendWeightsDependents +
					s.DependenciesScore * BlendWeightsDependencies +
					s.LOCScore * BlendWeightsLOC
				).GetEnumerator();
				while (importantTypes.Count < noRequested)
				{
					byBlendScore.MoveNext();
					importantTypes.Add(byBlendScore.Current.Node);
				}

				return importantTypes;
			}

			private record NodeScores(NodeKey Node)
			{
				public double DependentsScore { get; set; }
				public double DependenciesScore { get; set; }
				public double LOCScore { get; set; }
			}
		}
	}
}
