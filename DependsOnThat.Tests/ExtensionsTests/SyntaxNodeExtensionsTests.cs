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
				var symbols = rootNode.GetAllReferencedTypeSymbols(model, includeExternalMetadata: false).ToArray();

				AssertEx.Contains(symbols, s => s.Name == "EnumerableExtensions");
			}
		}

		[Test]
		public async Task When_Implicit_Var()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeDeeperClass.cs");
				var rootNode = await someClassTree.GetRootAsync();
				var model = compilation.GetSemanticModel(someClassTree);
				var symbols = rootNode.GetAllReferencedTypeSymbols(model, includeExternalMetadata: false).ToArray();

				AssertEx.Contains(symbols, s => s.Name == "SomeClassAsImplicitVar");
			}
		}

		[Test]
		public async Task When_Generic_Type_Argument()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeClassUsingConstructedTypes.cs");
				var rootNode = await someClassTree.GetRootAsync();
				var model = compilation.GetSemanticModel(someClassTree);
				var symbols = rootNode.GetAllReferencedTypeSymbols(model, includeExternalMetadata: false).ToArray();

				AssertEx.Contains(symbols, s => s.Name == "SomeClassInGenericType");
			}
		}

		[Test]
		public async Task When_Array_Element_Type()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeClassUsingConstructedTypes.cs");
				var rootNode = await someClassTree.GetRootAsync();
				var model = compilation.GetSemanticModel(someClassTree);
				var symbols = rootNode.GetAllReferencedTypeSymbols(model, includeExternalMetadata: false).ToArray();

				AssertEx.Contains(symbols, s => s.Name == "SomeClassInArray");
			}
		}

		[Test]
		public async Task When_Unnamed_Tuple_Element()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeClassUsingConstructedTypes.cs");
				var rootNode = await someClassTree.GetRootAsync();
				var model = compilation.GetSemanticModel(someClassTree);
				var symbols = rootNode.GetAllReferencedTypeSymbols(model, includeExternalMetadata: false).ToArray();

				AssertEx.Contains(symbols, s => s.Name == "SomeClassInTuple1");
				AssertEx.None(symbols, s => (s as INamedTypeSymbol)?.TupleElements.GetLengthSafe() > 0);
			}
		}

		[Test]
		public async Task When_Named_Tuple_Element()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeClassUsingConstructedTypes.cs");
				var rootNode = await someClassTree.GetRootAsync();
				var model = compilation.GetSemanticModel(someClassTree);
				var symbols = rootNode.GetAllReferencedTypeSymbols(model, includeExternalMetadata: false).ToArray();

				AssertEx.Contains(symbols, s => s.Name == "SomeClassInTuple2");
				AssertEx.None(symbols, s => (s as INamedTypeSymbol)?.TupleElements.GetLengthSafe() > 0);
			}
		}

		[Test]
		public async Task When_Type_Argument_Nested_Deeply()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeClassUsingConstructedTypes.cs");
				var rootNode = await someClassTree.GetRootAsync();
				var model = compilation.GetSemanticModel(someClassTree);
				var symbols = rootNode.GetAllReferencedTypeSymbols(model, includeExternalMetadata: false).ToArray();

				AssertEx.Contains(symbols, s => s.Name == "SomeClassInTuple3");
			}
		}

		[Test]
		public async Task When_Generic_Class_Open()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeClassUsingConstructedTypes.cs");
				var rootNode = await someClassTree.GetRootAsync();
				var model = compilation.GetSemanticModel(someClassTree);
				var symbols = rootNode.GetAllReferencedTypeSymbols(model, includeExternalMetadata: false).ToArray();

				AssertEx.Contains(symbols, s => s.Name == "SomeGenericClass1");
			}
		}

		[Test]
		public async Task When_Generic_Class_Closed()
		{
			using (var workspace = WorkspaceUtils.GetSubjectSolution())
			{
				var project = workspace.CurrentSolution.Projects.Single(p => p.Name == "SubjectSolution");
				var compilation = await project.GetCompilationAsync();
				var someClassTree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "SomeClassUsingConstructedTypes.cs");
				var rootNode = await someClassTree.GetRootAsync();
				var model = compilation.GetSemanticModel(someClassTree);
				var symbols = rootNode.GetAllReferencedTypeSymbols(model, includeExternalMetadata: false).ToArray();

				AssertEx.Contains(symbols, s => s.Name == "SomeGenericClass2");
			}
		}
	}
}
