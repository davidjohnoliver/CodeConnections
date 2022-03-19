#nullable enable

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

		public abstract IEnumerable<NodeKey> GetImportantTypes(NodeGraph fullGraph, IntOrAuto noRequested);

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

		private abstract class SimpleClassifier : ImportantTypesClassifier
		{
			public override sealed IEnumerable<NodeKey> GetImportantTypes(NodeGraph fullGraph, IntOrAuto noRequested)
				=> fullGraph.Nodes.Values.OrderByDescending(n => GetScore(n)).Select(n => n.Key).Take(GetNodesRequestedFromAuto(fullGraph, noRequested));

			public abstract double GetScore(Node node);
		}
	}
}
