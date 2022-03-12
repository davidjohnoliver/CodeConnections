#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeConnections.Graph
{
	public abstract partial class ImportantTypesClassifier
	{
		public ImportantTypesMode Mode { get; private set; }
		private ImportantTypesClassifier()
		{

		}

		public abstract IEnumerable<NodeKey> GetImportantTypes(NodeGraph fullGraph, int noRequested);

		public static ImportantTypesClassifier GetForMode(ImportantTypesMode mode)
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

			return classifier;
		}

		private abstract class SimpleClassifier : ImportantTypesClassifier
		{
			public override sealed IEnumerable<NodeKey> GetImportantTypes(NodeGraph fullGraph, int noRequested)
				=> fullGraph.Nodes.Values.OrderByDescending(n => GetScore(n)).Select(n => n.Key).Take(noRequested);

			public abstract double GetScore(Node node);
		}
	}
}
