using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using DependsOnThat.Graph;
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

				var fullGraph = await NodeGraph.BuildGraphFromRoots(
					new[] {
						(await someClassTree.GetRootAsync()).GetAllDeclaredTypes(compilation.GetSemanticModel(someClassTree)).First(),
						(await someCircularClassTree.GetRootAsync()).GetAllDeclaredTypes(compilation.GetSemanticModel(someCircularClassTree)).First()
					},
					workspace.CurrentSolution,
					ct: default);

				var displayGraph = fullGraph.GetDisplaySubgraph(extensionDepth: 0);
				Assert.AreEqual(2, displayGraph.VertexCount);
				Assert.AreEqual(0, displayGraph.EdgeCount);
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

				var fullGraph = await NodeGraph.BuildGraphFromRoots(
					new[] {
						(await someClassTree.GetRootAsync()).GetAllDeclaredTypes(compilation.GetSemanticModel(someClassTree)).First(),
						(await someCircularClassTree.GetRootAsync()).GetAllDeclaredTypes(compilation.GetSemanticModel(someCircularClassTree)).First()
					},
					workspace.CurrentSolution,
					ct: default);

				var displayGraph = fullGraph.GetDisplaySubgraph(extensionDepth: 1);
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

				var fullGraph = await NodeGraph.BuildGraphFromRoots(
					new[] {
						(await someClassTree.GetRootAsync()).GetAllDeclaredTypes(compilation.GetSemanticModel(someClassTree)).First(),
					},
					workspace.CurrentSolution,
					ct: default);

				var displayGraph = fullGraph.GetDisplaySubgraph(extensionDepth: 1);
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

				var fullGraph = await NodeGraph.BuildGraphFromRoots(
					new[] {
						(await someClassTree.GetRootAsync()).GetAllDeclaredTypes(compilation.GetSemanticModel(someClassTree)).First(),
					},
					workspace.CurrentSolution,
					ct: default);

				var displayGraph = fullGraph.GetDisplaySubgraph(extensionDepth: 2);
				Assert.AreEqual(9, displayGraph.VertexCount);
				Assert.AreEqual(9, displayGraph.EdgeCount);
			}
		}
	}
}
