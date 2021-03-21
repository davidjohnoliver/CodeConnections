#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Extensions;
using CodeConnections.Graph;
using CodeConnections.Roslyn;
using CodeConnections.Tests.Utilities;
using CodeConnections.Utilities;
using NUnit.Framework;

namespace CodeConnections.Tests.GraphTests
{
	[TestFixture]
	public partial class NodeGraphTests
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

				var graph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);

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

				AssertConsistentState(graph);
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

				var graph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);

				var root = graph.GetNodeForType(classSymbol);
				Assert.AreEqual(classSymbol.ToIdentifier(), root.Identifier);

				Assert.AreEqual(3, root.ForwardLinks.Count);
				Assert.AreEqual(2, root.BackLinks.Count);

				AssertConsistentState(graph);
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

				var graph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);

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
				var graph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);

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

		[Test]
		public async Task When_Project_Excluded()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var fullGraph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);
				AssertEx.Contains(fullGraph.Nodes, n => (n.Value as TypeNode)?.Identifier.Name == "SomeClassCore");

				var mainProject = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");

				var graphWithCoreExcluded = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), new[] { mainProject.ToIdentifier() });

				AssertEx.None(graphWithCoreExcluded.Nodes, n => (n.Value as TypeNode)?.Identifier.Name == "SomeClassCore");
			}
		}

		[Test]
		public async Task When_Purely_Generated_Excluded()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var fullGraph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);
				AssertEx.Contains(fullGraph.Nodes, n => (n.Value as TypeNode)?.Identifier.Name == "SomeGeneratedClass");
				AssertEx.Contains(fullGraph.Nodes, n => (n.Value as TypeNode)?.Identifier.Name == "SomePartiallyGeneratedClass");

				var graphWithGeneratedExcluded = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), excludePureGenerated: true);

				AssertEx.None(graphWithGeneratedExcluded.Nodes, n => (n.Value as TypeNode)?.Identifier.Name == "SomeGeneratedClass");
				AssertEx.Contains(graphWithGeneratedExcluded.Nodes, n => (n.Value as TypeNode)?.Identifier.Name == "SomePartiallyGeneratedClass");
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

		private static void AssertConsistentState(NodeGraph nodeGraph)
		{
			AssertNoDuplicates(nodeGraph);
			AssertNoLooseLinks(nodeGraph);
			AssertNoSelfLinks(nodeGraph);
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

		private static void AssertNoSelfLinks(NodeGraph nodeGraph)
		{
			AssertEx.None(GetAllNodes(nodeGraph), n => n is TypeNode tn && tn.AllLinks().Contains(n));
		}

		private static IEnumerable<Node> GetAllNodes(NodeGraph graph) => graph.Nodes.Values;
	}
}
