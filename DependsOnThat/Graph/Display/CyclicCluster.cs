#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Presentation;
using QuickGraph;

namespace DependsOnThat.Graph.Display
{
	/// <summary>
	/// Represents a cluster of <see cref="DisplayNode"/>s linked by mutual dependency cycles, ie any node in the cluster depends on every 
	/// other node in the cluster.
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplayString}")]
	public class CyclicCluster : BidirectionalGraph<DisplayNode, DisplayEdge>
	{
		/// <summary>
		/// The sort layer is defined as:
		/// SL = 0 for types with no dependents
		/// SL = Max(SL_0...SL_N) + 1, where SL_n is the sort layer of the nth direct dependent
		/// </summary>
		public int SortLayer { get; internal set; }

		/// <summary>
		/// Defined as:
		/// SLT = Max(<see cref="SortLayer"/>) for types with no dependencies
		/// SLT = Max(SLT_0...SLT_N) - 1, where SLT_n is SortLayerFromTop for nth direct dependency
		/// </summary>
		public int SortLayerFromTop { get; internal set; }

		private string DebuggerDisplayString
		{
			get
			{
				var baseStr = VertexCount == 1 ?
					$"{{ {Vertices.Single()} }}" :
					$"VertexCount={VertexCount}, EdgeCount={EdgeCount}";
				return $"{nameof(CyclicCluster)}-{baseStr}, SortLayer={SortLayer}, SortLayerFromTop={SortLayerFromTop}";
			}
		}
	}
}
