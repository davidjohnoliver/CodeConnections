using CodeConnections.Graph;
using CodeConnections.Roslyn;
using CodeConnections.Tests.Extensions;
using CodeConnections.Tests.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeConnections.Tests.GraphTests
{
	[TestFixture]
	public class SubgraphOperationsTests
	{
		[Test]
		public async Task When_AddNonpublicDependenciesOp_Public_Root()
		{
			// Expected classes: AA, AB, AE, AF, AG, AGInner (must exclude AC and AD)
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var graph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);

				var rootNode = graph.GetNodeForType("AA");

				var op = Subgraph.AddNonpublicDependenciesOp(rootNode.Key);
				var subgraph = CreateEmptySubgraph();
				var modifiedFirst = await op.Apply(subgraph, graph, CancellationToken.None);
				Assert.IsTrue(modifiedFirst);
				;
				var expectedNodes = new[] { "AA", "AB", "AE", "AF", "AG", "AGInner" }.Select(n => graph.GetNodeForType(n).Key).ToArray();
				CollectionAssert.AreEquivalent(expectedNodes, subgraph.AllNodes);

				var modifiedSecond = await op.Apply(subgraph, graph, CancellationToken.None);
				Assert.IsFalse(modifiedSecond);
				CollectionAssert.AreEquivalent(expectedNodes, subgraph.AllNodes);
			}
		}

		[Test]
		public async Task When_AddIndirectDependenciesOp()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var graph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);

				var rootNode = graph.GetNodeForType("AA");

				var op = Subgraph.AddIndirectDependenciesOp(rootNode.Key);
				var subgraph = CreateEmptySubgraph();
				var modifiedFirst = await op.Apply(subgraph, graph, CancellationToken.None);
				Assert.IsTrue(modifiedFirst);
				;
				var expectedNodes = new[] { "AA", "AB", "AC", "AD", "AE", "AF", "AG", "AGInner" }.Select(n => graph.GetNodeForType(n).Key).ToArray();
				CollectionAssert.AreEquivalent(expectedNodes, subgraph.AllNodes);

				var modifiedSecond = await op.Apply(subgraph, graph, CancellationToken.None);
				Assert.IsFalse(modifiedSecond);
				CollectionAssert.AreEquivalent(expectedNodes, subgraph.AllNodes);
			}
		}
		
		[Test]
		public async Task When_AddDirectDependenciesOp()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var graph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);

				var rootNode = graph.GetNodeForType("AA");

				var op = Subgraph.AddDirectDependenciesOp(rootNode.Key);
				var subgraph = CreateEmptySubgraph();
				var modifiedFirst = await op.Apply(subgraph, graph, CancellationToken.None);
				Assert.IsTrue(modifiedFirst);
				;
				var expectedNodes = new[] { "AA", "AB", "AC", "AE"}.Select(n => graph.GetNodeForType(n).Key).ToArray();
				CollectionAssert.AreEquivalent(expectedNodes, subgraph.AllNodes);

				var modifiedSecond = await op.Apply(subgraph, graph, CancellationToken.None);
				Assert.IsFalse(modifiedSecond);
				CollectionAssert.AreEquivalent(expectedNodes, subgraph.AllNodes);
			}
		}

		private static Subgraph CreateEmptySubgraph() => new Subgraph((_, _) => true);
	}
}
