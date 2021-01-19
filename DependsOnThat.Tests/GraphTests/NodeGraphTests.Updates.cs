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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace DependsOnThat.Tests.GraphTests
{
	partial class NodeGraphTests
	{
		[Test]
		public async Task When_Updated_Invariant()
		{

			using var workspace = WorkspaceUtils.GetSubjectSolution();

			var fullGraph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);

			var initialState = fullGraph.Nodes.ToDictionary(kvp => kvp.Key, kvp => (kvp.Value.BackLinks.ToList(), kvp.Value.ForwardLinks.ToList()));

			await fullGraph.Update(CompilationCache.CacheWithSolution(workspace.CurrentSolution), workspace.CurrentSolution.GetAllDocumentIds(), default);

			Assert.AreEqual(initialState.Count, fullGraph.Nodes.Count);

			foreach (var kvp in fullGraph.Nodes)
			{
				Assert.IsTrue(initialState.ContainsKey(kvp.Key));
				var initialNodeState = initialState[kvp.Key];
				var node = kvp.Value;
				CollectionAssert.AreEqual(initialNodeState.Item1, node.BackLinks);
				CollectionAssert.AreEqual(initialNodeState.Item2, node.ForwardLinks);
			}

			AssertConsistentState(fullGraph);
		}

		[Test]
		public async Task When_Updated_Reference_Added()
		{
			using var workspace = WorkspaceUtils.GetSubjectSolution();

			var fullGraph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);

			var mutableClassKey = TypeNodeKey.GetFromFullName("SubjectSolution.Mutable.SomeMutableClass");
			var mutableClassNode = (TypeNode)fullGraph.Nodes[mutableClassKey];

			Assert.AreEqual(1, mutableClassNode.ForwardLinks.Count);

			var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
			var mutableDoc = project.Documents.Single(d => d.FilePath.EndsWith("SomeMutableClass.cs"));

			var mutableRoot = await mutableDoc.GetSyntaxRootAsync();
			var replaceableMethod = mutableRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "MightBeReplaced");

			var replacementText = @"
		public void MightBeReplaced()
		{
			var replacement = new SomeClassAddableReference();
		}
";

			var mutableDocText = await mutableDoc.GetTextAsync();
			var mutatedMutableDocText = mutableDocText.Replace(replaceableMethod.Span, replacementText);
			var mutatedMutableDoc = mutableDoc.WithText(mutatedMutableDocText);
			Assert.AreEqual(mutableDoc.Id, mutatedMutableDoc.Id);

			var mutatedSolution = mutatedMutableDoc.Project.Solution;

			await fullGraph.Update(CompilationCache.CacheWithSolution(mutatedSolution), new[] { mutableDoc.Id }, default);

			var mutatedmutableClassNode = (TypeNode)fullGraph.Nodes[mutableClassKey]; // For now this is reference-equal to mutableClassNode, but let's try not to rely on it
			Assert.AreEqual(2, mutableClassNode.ForwardLinks.Count);
			AssertEx.Contains(mutableClassNode.ForwardLinks, n => (n as TypeNode).Identifier.Name == "SomeClassAddableReference");

			AssertConsistentState(fullGraph);
		}

		[Test]
		public async Task When_Updated_Reference_Removed()
		{
			using var workspace = WorkspaceUtils.GetSubjectSolution();

			var fullGraph = await NodeGraph.BuildGraph(CompilationCache.CacheWithSolution(workspace.CurrentSolution), ct: default);

			var mutableClassKey = TypeNodeKey.GetFromFullName("SubjectSolution.Mutable.SomeMutableClass");
			var mutableClassNode = (TypeNode)fullGraph.Nodes[mutableClassKey];

			Assert.AreEqual(1, mutableClassNode.ForwardLinks.Count);
			var removableReferenceNode = AssertEx.Contains(mutableClassNode.ForwardLinks, n => (n as TypeNode).Identifier.Name == "SomeClassRemovableReference");
			AssertEx.Contains(removableReferenceNode.BackLinks, mutableClassNode);

			var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
			var mutableDoc = project.Documents.Single(d => d.FilePath.EndsWith("SomeMutableClass.cs"));

			var mutableRoot = await mutableDoc.GetSyntaxRootAsync();
			var removableMethod = mutableRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "MightBeRemoved");

			var mutableDocText = await mutableDoc.GetTextAsync();
			var mutatedMutableDocText = mutableDocText.Replace(removableMethod.Span, "");
			var mutatedMutableDoc = mutableDoc.WithText(mutatedMutableDocText);
			Assert.AreEqual(mutableDoc.Id, mutatedMutableDoc.Id);

			var mutatedSolution = mutatedMutableDoc.Project.Solution;

			await fullGraph.Update(CompilationCache.CacheWithSolution(mutatedSolution), new[] { mutableDoc.Id }, default);

			var mutatedmutableClassNode = (TypeNode)fullGraph.Nodes[mutableClassKey]; // For now this is reference-equal to mutableClassNode, but let's try not to rely on it
			Assert.AreEqual(0, mutatedmutableClassNode.ForwardLinks.Count);
			AssertEx.None(removableReferenceNode.BackLinks, n => (n as TypeNode).Identifier.Name == "SomeMutableClass");
		}
	}
}
