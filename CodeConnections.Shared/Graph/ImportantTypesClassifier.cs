#nullable enable

using CodeConnections.Collections;
using CodeConnections.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeConnections.Graph
{
	public abstract partial class ImportantTypesClassifier
	{
		public ImportantTypesMode Mode { get; private set; }

		private int _maxNodesInAuto;

		private ImportantTypesClassifier()
		{

		}

		public abstract IEnumerable<(NodeKey, Importance)> GetImportantTypes(NodeGraph fullGraph, IntOrAuto noRequested);

		private int GetAutoNodesCount(NodeGraph fullGraph)
		{
			const int minNodes = 10;
			const double typesFraction = 0.10;

			var maxNodes = Math.Max(minNodes, (int)(_maxNodesInAuto * 0.95));

			var autoTarget = (int)(fullGraph.Nodes.Count * typesFraction);

			return MathUtils.Clamp(autoTarget, minNodes, maxNodes);
		}

		private int GetNodesRequestedFromAuto(NodeGraph fullGraph, IntOrAuto nodesRequested)
			=> nodesRequested.IsAuto ? GetAutoNodesCount(fullGraph) : nodesRequested.Value;

		public static ImportantTypesClassifier GetForMode(ImportantTypesMode mode, int maxNodes)
		{
			ImportantTypesClassifier classifier = mode switch
			{
				ImportantTypesMode.HouseBlend => new HouseBlendClassifier(),
				ImportantTypesMode.MostDependencies => new DependenciesClassifier(),
				ImportantTypesMode.MostDependents => new DependentsClassifier(),
				ImportantTypesMode.MostLOC => new LOCClassifier(),
				_ => throw new ArgumentException()
			};
			classifier.Mode = mode;
			classifier._maxNodesInAuto = maxNodes;

			return classifier;
		}

		protected const double GoldFraction = 0.2;
		protected const double SilverFraction = 0.4;

		private abstract class SimpleClassifier : ImportantTypesClassifier, IScoreProvider
		{
			public override sealed IEnumerable<(NodeKey, Importance)> GetImportantTypes(NodeGraph fullGraph, IntOrAuto noRequested)
			{
				var importantTypes = fullGraph.Nodes.Values
					.OrderByDescending(n => GetScore(n))
					.Select(n => n.Key)
					.Take(GetNodesRequestedFromAuto(fullGraph, noRequested))
					.ToList();

				var golds = (int)(GoldFraction * importantTypes.Count);
				var goldsAndsilvers = (int)((GoldFraction + SilverFraction) * importantTypes.Count);
				var values = new CollectionDictionary<Importance, NodeKey>();

				for (int i = 0; i < golds; i++)
				{
					values[Importance.High].Add(importantTypes[i]);
				}

				for (int i = golds; i < goldsAndsilvers; i++)
				{
					values[Importance.Intermediate].Add(importantTypes[i]);
				}

				for (int i = goldsAndsilvers; i < importantTypes.Count; i++)
				{
					values[Importance.Low].Add(importantTypes[i]);
				}

				return values.SelectMany(kvp => kvp.Value.Select(n => (n, kvp.Key)));
			}

			public abstract double GetScore(Node node);
		}

		protected interface IScoreProvider
		{
			double GetScore(Node node);
		}
	}
}
