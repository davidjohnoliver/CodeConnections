#nullable disable

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

namespace DependsOnThat.Tests.GraphTests
{
	[TestFixture]
	public class NodeGraphTests
	{
		[Test]
		public async Task When_Building_From_Subject()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeClass.cs");
				var rootNode = await someClassTree.GetRootAsync();

				var model = compilation.GetSemanticModel(someClassTree);

				var classSymbol = rootNode.GetAllDeclaredTypes(model).First();
				Assert.AreEqual("SomeClass", classSymbol.Name);

				var graph = await NodeGraph.BuildGraphFromRoots(new[] { classSymbol }, workspace.CurrentSolution, ct: default);

				var root = graph.Roots.Single();
				Assert.AreEqual(classSymbol, (root as TypeNode).Symbol);
				Assert.AreEqual(4, root.ForwardLinks.Count);
				AssertEx.Contains(root.ForwardLinks, n => (n as TypeNode)?.Symbol.Name == "SomeEnumeratedClass");
				AssertEx.Contains(root.ForwardLinks, n => (n as TypeNode)?.Symbol.Name == "SomeClassCore");
				var someOtherClassNode = AssertEx.Contains(root.ForwardLinks, n => (n as TypeNode)?.Symbol.Name == "SomeOtherClass") as TypeNode;

				CollectionAssert.Contains(someOtherClassNode.BackLinks, root);
				AssertEx.Contains(someOtherClassNode.BackLinks, n => (n as TypeNode)?.Symbol.Name == "SomeCircularClass");
				AssertEx.Contains(someOtherClassNode.ForwardLinks, n => (n as TypeNode)?.Symbol.Name == "SomeClassInArray");
				AssertEx.Contains(someOtherClassNode.ForwardLinks, n => (n as TypeNode)?.Symbol.Name == "SomeDeeperClass");
			}
		}

		[Test]
		public async Task When_Root_With_BackLinks()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeOtherClass.cs");
				var rootNode = await someClassTree.GetRootAsync();

				var model = compilation.GetSemanticModel(someClassTree);

				var classSymbol = rootNode.GetAllDeclaredTypes(model).First();
				Assert.AreEqual("SomeOtherClass", classSymbol.Name);

				var graph = await NodeGraph.BuildGraphFromRoots(new[] { classSymbol }, workspace.CurrentSolution, ct: default);

				var root = graph.Roots.Single();
				Assert.AreEqual(classSymbol, (root as TypeNode).Symbol);

				Assert.AreEqual(3, root.ForwardLinks.Count);
				Assert.AreEqual(1, root.BackLinks.Count);
			}
		}

		[Test]
		public async Task When_Extension_Method()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeOtherClass.cs");
				var rootNode = await someClassTree.GetRootAsync();

				var model = compilation.GetSemanticModel(someClassTree);

				var classSymbol = rootNode.GetAllDeclaredTypes(model).First();
				Assert.AreEqual("SomeOtherClass", classSymbol.Name);

				var graph = await NodeGraph.BuildGraphFromRoots(new[] { classSymbol }, workspace.CurrentSolution, ct: default);

				var root = graph.Roots.Single();
				Assert.AreEqual(classSymbol, (root as TypeNode).Symbol);

				AssertEx.Contains(root.ForwardLinks, n => (n as TypeNode)?.Symbol.Name == "EnumerableExtensions");
			}
		}
	}
}
