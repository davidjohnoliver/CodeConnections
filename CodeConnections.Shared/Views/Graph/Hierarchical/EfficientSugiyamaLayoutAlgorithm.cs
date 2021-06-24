﻿// https://github.com/NinetailLabs/GraphSharp/tree/4831873c0465c0738adc94c7180a417352efeb58/Graph%23/Algorithms/Layout/Simple
#nullable enable

using System.Collections.Generic;
using System.Windows;
using GraphSharp.Algorithms.EdgeRouting;
using GraphSharp.Algorithms.Layout;
using QuickGraph;

namespace CodeConnections.Views.Graph.Hierarchical
{
	public partial class EfficientSugiyamaLayoutAlgorithm<TVertex, TEdge, TGraph>
		: StableLayoutAlgorithmBase<TVertex, TEdge, TGraph, EfficientSugiyamaLayoutParameters>,
		  IEdgeRoutingAlgorithm<TVertex, TEdge, TGraph>
		where TVertex : class
		where TEdge : IEdge<TVertex>
		where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>
	{
		/// <summary>The copy of the VisitedGraph which should be laid out.</summary>
		private IMutableBidirectionalGraph<SugiVertex, SugiEdge> _graph =  new BidirectionalGraph<SugiVertex, SugiEdge>();

		/// <summary>Routing points for the edges of the original graph.</summary>
		private readonly IDictionary<TEdge, Point[]> _edgeRoutingPoints = new Dictionary<TEdge, Point[]>();

		private readonly IDictionary<TEdge, IList<SugiVertex>> _dummyVerticesOfEdges = new Dictionary<TEdge, IList<SugiVertex>>();

		private readonly IDictionary<TVertex, Size> _vertexSizes;

		private readonly IDictionary<TVertex, SugiVertex> _vertexMap = new Dictionary<TVertex, SugiVertex>();

		/// <summary>Isolated vertices in the visited graph, which will be handled only in the last step of the layout.</summary>
		private List<SugiVertex>? _isolatedVertices;

		/// <summary>It stores the vertices or segments which inside the layers.</summary>
		private readonly IList<IList<SugiVertex>> _layers = new List<IList<SugiVertex>>();

		public EfficientSugiyamaLayoutAlgorithm(
			TGraph visitedGraph,
			EfficientSugiyamaLayoutParameters? parameters,
			IDictionary<TVertex, Point> vertexPositions,
			IDictionary<TVertex, Size> vertexSizes,
			int randomSeed)
			: base(visitedGraph, vertexPositions, parameters, randomSeed)
		{
			_vertexSizes = vertexSizes;

			_rnd = GetRandomWithCurrentSeed();
		}

		/// <summary>Initializes the private _graph field which stores the graph that we operate on.</summary>
		private void InitTheGraph()
		{
			// make a copy of the original graph
			_graph = new BidirectionalGraph<SugiVertex, SugiEdge>();

			// copy the vertices
			foreach (var vertex in VisitedGraph.Vertices)
			{
				var size = new Size();
				if (_vertexSizes != null)
					_vertexSizes.TryGetValue(vertex, out size);

				var vertexWrapper = new SugiVertex(vertex, size);
				_graph.AddVertex(vertexWrapper);
				_vertexMap[vertex] = vertexWrapper;
			}

			// copy the edges
			foreach (var edge in VisitedGraph.Edges)
			{
				var edgeWrapper = new SugiEdge(edge, _vertexMap[edge.Source], _vertexMap[edge.Target]);
				_graph.AddEdge(edgeWrapper);
			}
		}

		protected override void InternalCompute()
		{
			ResetSeedForCompute();

			InitTheGraph();

			DoPreparing();

			BuildSparseNormalizedGraph();
			DoCrossingMinimizations();
			CalculatePositions();
		}

		#region IEdgeRoutingAlgorithm<TVertex,TEdge,TGraph> Members

		public IDictionary<TEdge, Point[]> EdgeRoutes
		{
			get { return _edgeRoutingPoints; }
		}

		#endregion
	}
}