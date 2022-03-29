#nullable enable

using CodeConnections.Collections;
using CodeConnections.Extensions;
using CodeConnections.Utilities;
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
			/// <summary>
			/// The proportional occupancy assigned to each score category.
			/// </summary>
			private static readonly Dictionary<ScoreCategory, double> CategoryProportions = new()
			{
				{ ScoreCategory.Dependents, 1.2 },
				{ ScoreCategory.Dependencies, 1 },
				{ ScoreCategory.Combined, .5 },
			};

			/// <summary>
			/// The weights assigned to 'simple' scores when using them to calculate the combined score.
			/// </summary>
			private static readonly Dictionary<ScoreCategory, double> ScoreWeights = new()
			{
				{ ScoreCategory.Dependents, 1 },
				{ ScoreCategory.Dependencies, 1 },
				{ ScoreCategory.LOC, 1 },
			};

			private readonly IReadOnlyDictionary<ScoreCategory, IScoreProvider> _scoreProviders;
			public HouseBlendClassifier()
			{
				_scoreProviders = EnumUtils.GetValues<ScoreCategory>()
					.Except(ScoreCategory.Combined)
					.ToDictionary(t => t, t => GetScoreProvider(t));
			}

			public override IEnumerable<(NodeKey, Importance)> GetImportantTypes(NodeGraph fullGraph, IntOrAuto nodesRequested)
			{
				// 1. Calculate simple scores for all nodes
				var allScores = fullGraph.Nodes
					.Select(kvp =>
					{
						var score = new NodeScore(kvp.Key);
						foreach (var kvpSP in _scoreProviders)
						{
							score.Scores[kvpSP.Key] = kvpSP.Value.GetScore(kvp.Value);
						};
						return score;
					})
					.ToList();
				// 2. Normalize simple scores
				NormalizeScores(allScores);
				// 3. Calculate combined score
				foreach (var score in allScores)
				{
					CalculateCombinedScore(score);
				}
				// 4. Populate importance buckets, allotting by category according to category proportions
				var noRequested = GetNodesRequestedFromAuto(fullGraph, nodesRequested);
				noRequested = Math.Min(noRequested, allScores.Count);
				var goldsCount = (int)(GoldFraction * noRequested);
				var silversCount = (int)(SilverFraction * noRequested);
				var importantTypes = new CollectionDictionary<Importance, NodeKey>(true);
				var sortedScores = CategoryProportions.Keys.ToDictionary(
					k => k,

					k => allScores.OrderByDescending(ns => ns.Scores[k])
						// Note: We implicitly rely on the enumerator being a reference type, be wary if changing the type of the enumerable
						.GetEnumerator()
				);
				PopulateImportanceBucket(importantTypes[Importance.High], sortedScores, goldsCount);
				PopulateImportanceBucket(importantTypes[Importance.Intermediate], sortedScores, silversCount);
				var bronzesCount = noRequested - importantTypes.ItemsCount;
				PopulateImportanceBucket(importantTypes[Importance.Low], sortedScores, bronzesCount);

				return importantTypes.SelectMany(kvp => kvp.Value.Select(n => (n, kvp.Key)));
			}

			private void PopulateImportanceBucket(IList<NodeKey> entries, Dictionary<ScoreCategory, IEnumerator<NodeScore>> sortedScores, int targetCount)
			{
				// Can be thought of this way: an entry has a 'price' (which we set to the highest value in CategoryProportions). Every tick,
				// we credit each category an amount equal to its proportion. If it has enough saved up to 'buy' an entry, it does so. We
				// continue until the target count is reached or exceeded.
				var entryPrice = CategoryProportions.Values.Max();
				var funds = CategoryProportions.Keys.ToDictionary(
					k => k,
					_ => 0d
				);
				while (entries.Count < targetCount)
				{
					foreach (var category in CategoryProportions.Keys)
					{
						var current = funds[category] + CategoryProportions[category];
						if (current >= entryPrice)
						{
							current -= entryPrice;
							var enumerator = sortedScores[category];
							enumerator.MoveNext();
							entries.Add(enumerator.Current.Node);
						}
						funds[category] = current;
					}
				}
			}

			private static IScoreProvider GetScoreProvider(ScoreCategory scoreType)
			{
				if (GetModeFromScoreType(scoreType) is { } importantTypesMode &&
					GetForMode(importantTypesMode, int.MaxValue) is IScoreProvider scoreProvider
				)
				{
					return scoreProvider;
				}

				throw new ArgumentException();
			}

			private static void CalculateCombinedScore(NodeScore nodeScores)
			{
				nodeScores.Scores[ScoreCategory.Combined] = ScoreWeights.Sum(kvp => kvp.Value * nodeScores.Scores[kvp.Key]);
			}

			private void NormalizeScores(IList<NodeScore> scores)
			{
				var invAverages = _scoreProviders.Keys
					.ToDictionary(
						st => st,
						st =>
						{
							var total = scores.Sum(s => s.Scores[st]);
							return scores.Count / total;
						}
					);
				foreach (var score in scores)
				{
					foreach (var kvp in invAverages)
					{
						score.Scores[kvp.Key] = score.Scores[kvp.Key] * kvp.Value;
					}
				}
			}

			private static ImportantTypesMode? GetModeFromScoreType(ScoreCategory scoreType)
				=> scoreType switch
				{
					ScoreCategory.Dependents => ImportantTypesMode.MostDependents,
					ScoreCategory.Dependencies => ImportantTypesMode.MostDependencies,
					ScoreCategory.LOC => ImportantTypesMode.MostLOC,
					_ => null
				};

			private class NodeScore
			{
				public NodeScore(NodeKey node)
				{
					Node = node;
				}
				public NodeKey Node { get; }

				public IDictionary<ScoreCategory, double> Scores { get; } = new Dictionary<ScoreCategory, double>();
			}

			private enum ScoreCategory
			{
				Dependents,
				Dependencies,
				LOC,
				Combined
			}
		}
	}
}
