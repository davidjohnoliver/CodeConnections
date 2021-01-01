#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using DependsOnThat.Graph;
using DependsOnThat.Graph.Display;
using DependsOnThat.Tests.Utilities;
using NUnit.Framework;

namespace DependsOnThat.Tests.PresentationTests
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


				var fullGraph = await NodeGraph.BuildGraph(workspace.CurrentSolution,
					ct: default);

				var displayGraph = fullGraph.GetDisplaySubgraph(roots, extensionDepth: 0);
				Assert.AreEqual(2, displayGraph.VertexCount);
				var simpleEdges = displayGraph.Edges.OfType<SimpleDisplayEdge>().ToArray();
				var multiEdges = displayGraph.Edges.OfType<MultiDependencyDisplayEdge>().ToArray();
				Assert.AreEqual(0, simpleEdges.Length);
				Assert.AreEqual(1, multiEdges.Length);
			}
		}

		[Test]
		public async Task When_Display_Graph_Two_Roots_Extension_One()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeClass.cs");
				var someCircularClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeCircularClass.cs");

				var roots = new[] {
						(await someClassTree.GetRootAsync()).GetAllDeclaredTypes(compilation.GetSemanticModel(someClassTree)).First(),
						(await someCircularClassTree.GetRootAsync()).GetAllDeclaredTypes(compilation.GetSemanticModel(someCircularClassTree)).First()
					};
				var fullGraph = await NodeGraph.BuildGraph(
					workspace.CurrentSolution,
					ct: default);

				var displayGraph = fullGraph.GetDisplaySubgraph(roots, extensionDepth: 1);
				Assert.AreEqual(7, displayGraph.VertexCount);
				Assert.AreEqual(7, displayGraph.EdgeCount);
			}
		}

		[Test]
		public async Task When_Display_Graph_One_Root_Extension_One()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeClass.cs");

				var roots = new[] {
						(await someClassTree.GetRootAsync()).GetAllDeclaredTypes(compilation.GetSemanticModel(someClassTree)).First(),
					};

				var fullGraph = await NodeGraph.BuildGraph(
					workspace.CurrentSolution,
					ct: default);

				var displayGraph = fullGraph.GetDisplaySubgraph(roots, extensionDepth: 1);
				Assert.AreEqual(5, displayGraph.VertexCount);
				Assert.AreEqual(4, displayGraph.EdgeCount);
			}
		}

		[Test]
		public async Task When_Display_Graph_One_Root_Extension_Two()
		{

			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeClass.cs");

				var roots = new[] {
						(await someClassTree.GetRootAsync()).GetAllDeclaredTypes(compilation.GetSemanticModel(someClassTree)).First(),
					};

				var fullGraph = await NodeGraph.BuildGraph(
					workspace.CurrentSolution,
					ct: default);

				var displayGraph = fullGraph.GetDisplaySubgraph(roots, extensionDepth: 2);
				Assert.AreEqual(10, displayGraph.VertexCount);
				Assert.AreEqual(10, displayGraph.EdgeCount);
			}
		}

		[Test]
		public async Task When_MultiDependency_Edge()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");

				var graph = await NodeGraph.BuildGraph(workspace.CurrentSolution, ct: default);

				var someClassNode = graph.Nodes[TypeNodeKey.GetFromFullName("SubjectSolution.SomeClass")];
				var deepClassNode = graph.Nodes[TypeNodeKey.GetFromFullName("SubjectSolution.SomeClassDepth5")];

				var roots = new HashSet<Node>();
				roots.Add(someClassNode);
				roots.Add(deepClassNode);

				var paths = NodeGraphExtensions.GetMultiDependencyRootPaths(graph, roots).ToArray();
				var path = paths.Single();

				var displayGraph = graph.GetDisplaySubgraph(roots, 1);

				var multiEdges = displayGraph.Edges.OfType<MultiDependencyDisplayEdge>();

				AssertEx.ContainsSingle(multiEdges, m => m.Source.DisplayString.EndsWith("SomeOtherClass") && m.Target.DisplayString.EndsWith("SomeClassDepth4"));
			}
		}

		[Test]
		public async Task When_Two_Adjacent_Roots()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");

				var graph = await NodeGraph.BuildGraph(workspace.CurrentSolution, ct: default);

				var root1 = graph.Nodes[TypeNodeKey.GetFromFullName("SubjectSolution.SomeClassDepth3")];
				var root2 = graph.Nodes[TypeNodeKey.GetFromFullName("SubjectSolution.SomeClassDepth4")];

				var roots = new HashSet<TypeNode>();
				roots.Add(root1 as TypeNode);
				roots.Add(root2 as TypeNode);

				var displayGraph = graph.GetDisplaySubgraph(roots, 1);

				AssertEx.Contains(displayGraph.Vertices, n => n.DisplayString.EndsWith("SomeDeeperClass"));
				AssertEx.Contains(displayGraph.Vertices, n => n.DisplayString.EndsWith("SomeClassDepth5"));
			}
		}
	}
}
