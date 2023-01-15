#nullable enable

using CodeConnections.Graph.Display;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _IDisplayGraph = QuickGraph.IBidirectionalGraph<CodeConnections.Graph.Display.DisplayNode, CodeConnections.Graph.Display.DisplayEdge>;

namespace CodeConnections.Export
{
	public static class MermaidHelper
	{
		/// <summary>
		/// Generate a Mermaid diagram from <paramref name="graph"/>. See https://mermaid.js.org/syntax/flowchart.html
		/// </summary>
		public static string GenerateMermaidGraph(_IDisplayGraph graph)
		{
			var mermaidIds = new Dictionary<DisplayNode, string>();
			int idCounter = -1;

			var sb = new StringBuilder();

			sb.AppendLine("graph BT");

			foreach (var edge in graph.Edges)
			{
				var mermaidEdge = $"    {GetVertexEntry(edge.Source)} --> {GetVertexEntry(edge.Target)}";
				sb.AppendLine(mermaidEdge);
			}

			// Append any vertices that don't have any edges
			foreach (var vertex in graph.Vertices.Except(mermaidIds.Keys))
			{
				var loneVertex = $"    {GetVertexEntry(vertex)}";
				sb.AppendLine(loneVertex);
			}

			return sb.ToString();

			string GetVertexEntry(DisplayNode vertex)
			{
				if (mermaidIds.TryGetValue(vertex, out var existing))
				{
					return existing;
				}

				idCounter++;

				var mermaidId = $"CC{idCounter}";
				mermaidIds[vertex] = mermaidId;

				return $"{mermaidId}(\"{Sanitize(vertex)}\")";
			}
		}

		private static string Sanitize(DisplayNode vertex)
		{
			var output = vertex.DisplayString;

			output = output.Replace("<", "&lt;");
			output = output.Replace(">", "&gt;");

			return output;
		}
	}
}
