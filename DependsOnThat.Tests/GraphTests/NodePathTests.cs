#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using DependsOnThat.Graph;
using DependsOnThat.Roslyn;
using DependsOnThat.Tests.Utilities;
using NUnit.Framework;

namespace DependsOnThat.Tests.GraphTests
{
	[TestFixture]
	public class NodePathTests
	{
		[Test]
		public async Task When_Two_Roots()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");

				var graph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);

				var someClassNode = graph.Nodes[TypeNodeKey.GetFromFullName("SubjectSolution.SomeClass")];
				var someCircularClassNode = graph.Nodes[TypeNodeKey.GetFromFullName("SubjectSolution.SomeCircularClass")];

				var roots = new HashSet<Node>();
				roots.Add(someClassNode);
				roots.Add(someCircularClassNode);

				var paths = NodeGraphExtensions.GetMultiDependencyRootPaths(graph, roots).ToArray();
				var path = paths.Single();

				Assert.AreEqual(someClassNode, path.Source);
				Assert.AreEqual(someCircularClassNode, path.Target);
				Assert.AreEqual(2, path.Intermediates.Count);
				Assert.AreEqual("SomeOtherClass", (path.Intermediates[0] as TypeNode).Identifier.Name);
				Assert.AreEqual("SomeDeeperClass", (path.Intermediates[1] as TypeNode).Identifier.Name);
			}
		}
		[Test]
		public async Task When_Two_Distant_Roots()
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

				Assert.AreEqual(someClassNode, path.Source);
				Assert.AreEqual(deepClassNode, path.Target);
				Assert.AreEqual(4, path.Intermediates.Count);
			}
		}
	}
}
