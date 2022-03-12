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
				AssertEx.Contains(root.ForwardLinkNodes, n => (n as TypeNode)?.Identifier.Name == "SomeEnumeratedClass");
				AssertEx.Contains(root.ForwardLinkNodes, n => (n as TypeNode)?.Identifier.Name == "SomeClassCore");
				var someOtherClassNode = AssertEx.Contains(root.ForwardLinkNodes, n => (n as TypeNode)?.Identifier.Name == "SomeOtherClass") as TypeNode;

				CollectionAssert.Contains(someOtherClassNode.BackLinkNodes, root);
				AssertEx.Contains(someOtherClassNode.BackLinkNodes, n => (n as TypeNode)?.Identifier.Name == "SomeCircularClass");
				AssertEx.Contains(someOtherClassNode.ForwardLinkNodes, n => (n as TypeNode)?.Identifier.Name == "SomeClassInArray");
				AssertEx.Contains(someOtherClassNode.ForwardLinkNodes, n => (n as TypeNode)?.Identifier.Name == "SomeDeeperClass");

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

				AssertEx.Contains(root.ForwardLinkNodes, n => (n as TypeNode)?.Identifier.Name == "EnumerableExtensions");
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

				var forwardLink = AssertEx.Contains(someClassNode.ForwardLinkNodes, n => n.Key.Equals(someClassCoreKey));
				Assert.AreEqual(someClassCoreNode, forwardLink);

				var backLink = AssertEx.Contains(someClassCoreNode.BackLinkNodes, n => n.Key.Equals(someClassKey));
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

		[Test]
		public async Task When_Purely_Generated_Excluded_And_Updated()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var fullGraph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);
				AssertEx.Contains(fullGraph.Nodes, n => (n.Value as TypeNode)?.Identifier.Name == "SomeGeneratedClass");
				AssertEx.Contains(fullGraph.Nodes, n => (n.Value as TypeNode)?.Identifier.Name == "SomePartiallyGeneratedClass");

				var graphWithGeneratedExcluded = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), excludePureGenerated: true);

				await graphWithGeneratedExcluded.Update(CompilationCache.CacheWithSolution(workspace.CurrentSolution), workspace.CurrentSolution.GetAllDocumentIds(), default);

				AssertEx.None(graphWithGeneratedExcluded.Nodes, n => (n.Value as TypeNode)?.Identifier.Name == "SomeGeneratedClass");
				AssertEx.Contains(graphWithGeneratedExcluded.Nodes, n => (n.Value as TypeNode)?.Identifier.Name == "SomePartiallyGeneratedClass");
			}
		}

		[Test]
		public async Task When_Inherited_Class()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var fullGraph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);
				var inheritedClassNode = fullGraph.Nodes.Values.Single(n => (n as TypeNode)?.Identifier.Name == "SomeInheritedClassDepth1");
				var baseClassLink = AssertEx.ContainsSingle(inheritedClassNode.ForwardLinks, l => (l.Dependency as TypeNode)?.Identifier.Name == "SomeBaseClass");
				Assert.AreEqual(LinkType.InheritsFromClass, baseClassLink.LinkType);
				var derivedClassLink = AssertEx.ContainsSingle(inheritedClassNode.BackLinks, l => (l.Dependent as TypeNode)?.Identifier.Name == "SomeInheritedClassDepth2");
				Assert.AreEqual(LinkType.InheritsFromClass, baseClassLink.LinkType);
			}
		}

		[Test]
		public async Task When_Inherited_Class_Generic()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var fullGraph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);
				var inheritedClassNode = fullGraph.Nodes.Values.Single(n => (n as TypeNode)?.Identifier.Name == "SomeInheritedConcretifiedClass");
				var baseClassLink = AssertEx.ContainsSingle(inheritedClassNode.ForwardLinks, l => (l.Dependency as TypeNode)?.Identifier.Name == "SomeBaseGenericClass<T>");
				Assert.AreEqual(LinkType.InheritsFromClass, baseClassLink.LinkType);
			}
		}

		[Test]
		public async Task When_Implemented_Interface()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var fullGraph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);
				var implementingClassNode = fullGraph.Nodes.Values.Single(n => (n as TypeNode)?.Identifier.Name == "SomeBaseClass");
				AssertImplements(implementingClassNode, "ISomeInheritedInterface");
				AssertImplements(implementingClassNode, "ISomeStillOtherInterface");
				AssertImplements(implementingClassNode, "ISomeGenericInterface<T>");

				void AssertImplements(Node implementer, string interfaceName)
				{
					var interfaceLink = AssertEx.ContainsSingle(implementer.ForwardLinks, l => (l.Dependency as TypeNode)?.Identifier.Name == interfaceName);
					Assert.AreEqual(LinkType.ImplementsInterface, interfaceLink.LinkType);
				}
			}
		}

		[Test]
		public async Task When_Delegates_Present()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var fullGraph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);
				var classNode = fullGraph.Nodes.Values.Single(n => (n as TypeNode)?.Identifier.Name == "SomeClassWithDelegate");
				Assert.AreEqual(0, classNode.ForwardLinks.Count);

				await fullGraph.Update(CompilationCache.CacheWithSolution(workspace.CurrentSolution), workspace.CurrentSolution.GetAllDocumentIds(), default);

				var classNodeAfterUpdate = fullGraph.Nodes.Values.Single(n => (n as TypeNode)?.Identifier.Name == "SomeClassWithDelegate");
				Assert.AreEqual(0, classNodeAfterUpdate.ForwardLinks.Count);
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
