#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GraphSharp.Algorithms.Layout;
using QuickGraph;

namespace CodeConnections.Views.Graph
{
	public abstract class StableLayoutAlgorithmBase<TVertex, TEdge, TGraph, TParam> : DefaultParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, TParam?>
		where TVertex : class
		where TEdge : IEdge<TVertex>
		where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>
		where TParam : class, ILayoutParameters, new()
	{
		/// <summary>
		/// The random seed.
		/// </summary>
		private readonly int _randomSeed;
		private int _seedIncrement = 0;

		protected StableLayoutAlgorithmBase(TGraph visitedGraph, IDictionary<TVertex, Point> vertexPositions, TParam? oldParameters, int randomSeed)
			: base(visitedGraph, vertexPositions, oldParameters)
		{
			_randomSeed = randomSeed;
		}

		protected void ResetSeedForCompute() => _seedIncrement = 0;

		protected Random GetRandomWithCurrentSeed() => new Random(_randomSeed + _seedIncrement++);

		protected override void InitializeWithRandomPositions(double width, double height, double translate_x, double translate_y)
		{

			var rnd = GetRandomWithCurrentSeed();

			//initialize with random position
			foreach (TVertex v in VisitedGraph.Vertices)
			{
				//for vertices without assigned position
				if (!VertexPositions.ContainsKey(v))
				{
					VertexPositions[v] =
						new Point(
							Math.Max(double.Epsilon, rnd.NextDouble() * width + translate_x),
							Math.Max(double.Epsilon, rnd.NextDouble() * height + translate_y));
				}
			}
		}
	}
}
