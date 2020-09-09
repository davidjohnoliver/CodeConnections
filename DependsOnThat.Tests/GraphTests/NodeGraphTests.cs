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
using DependsOnThat.Utilities;
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

				var graph = await NodeGraph.BuildGraph(workspace.CurrentSolution, ct: default);

				var root = graph.GetNodeForType(classSymbol);
				Assert.AreEqual(classSymbol.ToIdentifier(), root.Identifier);
				Assert.AreEqual(4, root.ForwardLinks.Count);
				AssertEx.Contains(root.ForwardLinks, n => (n as TypeNode)?.Identifier.Name == "SomeEnumeratedClass");
				AssertEx.Contains(root.ForwardLinks, n => (n as TypeNode)?.Identifier.Name == "SomeClassCore");
				var someOtherClassNode = AssertEx.Contains(root.ForwardLinks, n => (n as TypeNode)?.Identifier.Name == "SomeOtherClass") as TypeNode;

				CollectionAssert.Contains(someOtherClassNode.BackLinks, root);
				AssertEx.Contains(someOtherClassNode.BackLinks, n => (n as TypeNode)?.Identifier.Name == "SomeCircularClass");
				AssertEx.Contains(someOtherClassNode.ForwardLinks, n => (n as TypeNode)?.Identifier.Name == "SomeClassInArray");
				AssertEx.Contains(someOtherClassNode.ForwardLinks, n => (n as TypeNode)?.Identifier.Name == "SomeDeeperClass");

				AssertNoDuplicates(graph);
				AssertNoLooseLinks(graph);
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

				var graph = await NodeGraph.BuildGraph(workspace.CurrentSolution, ct: default);

				var root = graph.GetNodeForType(classSymbol);
				Assert.AreEqual(classSymbol.ToIdentifier(), root.Identifier);

				Assert.AreEqual(3, root.ForwardLinks.Count);
				Assert.AreEqual(2, root.BackLinks.Count);

				AssertNoDuplicates(graph);
				AssertNoLooseLinks(graph);
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

				var graph = await NodeGraph.BuildGraph(workspace.CurrentSolution, ct: default);

				var root = graph.GetNodeForType(classSymbol);
				Assert.AreEqual(classSymbol.ToIdentifier(), root.Identifier);

				AssertEx.Contains(root.ForwardLinks, n => (n as TypeNode)?.Identifier.Name == "EnumerableExtensions");
			}
		}

		[Test]
		public async Task When_Link_External_Assembly()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");

				var graph = await NodeGraph.BuildGraph(workspace.CurrentSolution, ct: default);

				var someClassKey = TypeNodeKey.GetFromFullName("SubjectSolution.SomeClass");
				var someClassNode = graph.Nodes[someClassKey];
				var someClassCoreKey = TypeNodeKey.GetFromFullName("SubjectSolution.Core.SomeClassCore");
				var someClassCoreNode = graph.Nodes[someClassCoreKey];

				var forwardLink = AssertEx.Contains(someClassNode.ForwardLinks, n => n.Key.Equals(someClassCoreKey));
				Assert.AreEqual(someClassCoreNode, forwardLink);

				var backLink = AssertEx.Contains(someClassCoreNode.BackLinks, n => n.Key.Equals(someClassKey));
				Assert.AreEqual(someClassNode, backLink);
			}
		}

		private static void AssertNoDuplicates(NodeGraph graph)
		{
			var nodes = GetAllNodes(graph).OfType<TypeNode>().ToList();

			var nodeNames = nodes.Select(n => n.Identifier.FullName).ToList();

			Assert.AreEqual(nodes.Count, nodeNames.Count);

			var distinctCount = nodeNames.Distinct().Count();
			Assert.AreEqual(nodes.Count, distinctCount);
		}

		private static void AssertNoLooseLinks(NodeGraph nodeGraph)
		{
			var nodes = nodeGraph.Nodes.Values.ToHashSet();
			var allLinks = GetAllNodes(nodeGraph).SelectMany(n => n.AllLinks()).ToList();
			foreach (var link in allLinks)
			{
				Assert.IsTrue(nodes.Contains(link), $"Link {link} not found in nodes");
			}
		}

		private static IEnumerable<Node> GetAllNodes(NodeGraph graph) => graph.Nodes.Values;
	}
}
