#nullable disable

using DependsOnThat.Extensions;
using DependsOnThat.Tests.Utilities;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Tests.ExtensionsTests
{
	[TestFixture]
	public class SyntaxNodeExtensionsTests
	{
		[Test]
		public async Task When_All_Symbols()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeClass.cs");
				var rootNode = await someClassTree.GetRootAsync();
				var symbols = rootNode.GetAllReferencedTypeSymbols(compilation.GetSemanticModel(someClassTree), includeExternalMetadata: false).ToArray();

				Assert.IsTrue(symbols.Any(s => s.Name == "SomeOtherClass"));
				Assert.IsTrue(symbols.Any(s => s.Name == "SomeEnumeratedClass"));
				Assert.IsTrue(symbols.Any(s => s.Name == "SomeClassCore"));
			}
		}
	}
}
