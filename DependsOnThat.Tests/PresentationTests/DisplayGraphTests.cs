#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Extensions;
using CodeConnections.Graph;
using CodeConnections.Graph.Display;
using CodeConnections.Roslyn;
using CodeConnections.Tests.Utilities;
using CodeConnections.Utilities;
using NUnit.Framework;

namespace CodeConnections.Tests.PresentationTests
{
	[TestFixture]
	public class DisplayGraphTests
	{
		[Test]
		public async Task When_Display_Graph_Two_Roots_No_Extension()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeClass.cs");
				var someCircularClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeCircularClass.cs");

				var roots =
					new[] {
						(await someClassTree.GetRootAsync()).GetAllDeclaredTypes(compilation.GetSemanticModel(someClassTree)).First(),
						(await someCircularClassTree.GetRootAsync()).GetAllDeclaredTypes(compilation.GetSemanticModel(someCircularClassTree)).First()
					};


				var fullGraph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution),
					ct: default);

				var displayGraph = fullGraph.GetDisplaySubgraph(roots);
				Assert.AreEqual(2, displayGraph.VertexCount);
				var simpleEdges = displayGraph.Edges.OfType<SimpleDisplayEdge>().ToArray();
				var multiEdges = displayGraph.Edges.OfType<MultiDependencyDisplayEdge>().ToArray();
				Assert.AreEqual(0, simpleEdges.Length);
				Assert.AreEqual(1, multiEdges.Length);
			}
		}

		[Test]
		public async Task When_MultiDependency_Edge()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");

				var graph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);

				var someClassNode = graph.Nodes[TypeNodeKey.GetFromFullName("SubjectSolution.SomeClass")];
				var deepClassNode = graph.Nodes[TypeNodeKey.GetFromFullName("SubjectSolution.SomeClassDepth5")];

				var roots = new HashSet<Node>();
				roots.Add(someClassNode);
				roots.Add(deepClassNode);

				var paths = NodeGraphExtensions.GetMultiDependencyRootPaths(graph, roots).ToArray();
				var path = paths.Single();

				var displayGraph = graph.GetDisplaySubgraph(subgraphNodes:roots, pinnedNodes: SetUtils.GetEmpty<NodeKey>());

				var multiEdges = displayGraph.Edges.OfType<MultiDependencyDisplayEdge>();

				AssertEx.ContainsSingle(multiEdges, m => m.Source.DisplayString.EndsWith("SomeClass") && m.Target.DisplayString.EndsWith("SomeClassDepth5"));
			}
		}
	}
}
