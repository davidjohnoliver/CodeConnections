#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Views.Graph.FDP;
using CodeConnections.Views.Graph.Hierarchical;
using GraphSharp.Algorithms;
using GraphSharp.Algorithms.Layout;
using QuickGraph;

namespace CodeConnections.Views.Graph
{
	/// <summary>
	/// Custom algorithm factory, using local modified versions of algorithms
	/// </summary>
	public class CCLayoutAlgorithmFactory<TVertex, TEdge, TGraph> : ILayoutAlgorithmFactory<TVertex, TEdge, TGraph>
		where TVertex : class
		where TEdge : IEdge<TVertex>
		where TGraph : class?, IBidirectionalGraph<TVertex, TEdge>?
	{
		public IEnumerable<string> AlgorithmTypes => new[] { "LinLog", "EfficientSugiyama" };

		public ILayoutAlgorithm<TVertex, TEdge, TGraph>? CreateAlgorithm(string newAlgorithmType, ILayoutContext<TVertex, TEdge, TGraph> context, ILayoutParameters parameters)
		{
			if (context == null || context.Graph == null)
				return null;

			if (context.Mode == LayoutMode.Simple)
			{
				switch (newAlgorithmType)
				{
					case "LinLog":
						return new LinLogLayoutAlgorithm<TVertex, TEdge, TGraph>(context.Graph, context.Positions,
																				 parameters as LinLogLayoutParameters);
					case "EfficientSugiyama":
						return new EfficientSugiyamaLayoutAlgorithm<TVertex, TEdge, TGraph>(context.Graph,
																							parameters as EfficientSugiyamaLayoutParameters,
																							context.Positions,
																							context.Sizes);
					default:
						return null;
				}
			}

			return null;
		}

		public ILayoutParameters? CreateParameters(string algorithmType, ILayoutParameters oldParameters)
		{
			switch (algorithmType)
			{
				case "LinLog":
					return oldParameters.CreateNewParameter<LinLogLayoutParameters>();
				case "EfficientSugiyama":
					return oldParameters.CreateNewParameter<EfficientSugiyamaLayoutParameters>();
				default:
					return null;
			}
		}

		public bool IsValidAlgorithm(string algorithmType) => AlgorithmTypes.Contains(algorithmType);

		public string GetAlgorithmType(ILayoutAlgorithm<TVertex, TEdge, TGraph> algorithm)
		{
			if (algorithm == null)
				return string.Empty;

			int index = algorithm.GetType().Name.IndexOf("LayoutAlgorithm");
			if (index == -1)
				return string.Empty;

			string algoType = algorithm.GetType().Name;
			return algoType.Substring(0, algoType.Length - index);
		}

		public bool NeedEdgeRouting(string algorithmType) => algorithmType != "EfficientSugiyama";

		public bool NeedOverlapRemoval(string algorithmType) => algorithmType != "EfficientSugiyama";
	}
}
