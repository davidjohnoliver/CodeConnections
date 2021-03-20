using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using DependsOnThat.Tests.Extensions;
using DependsOnThat.Tests.Utilities;
using NUnit.Framework;

namespace DependsOnThat.Tests.ExtensionsTests
{
	[TestFixture]
	public class SymbolExtensionsTests
	{
		[Test]
		public void When_Preferred_Declaration()
		{
			var declarations = new[]
			{
				"",
				"C:/Some/Path/SomeClass.cs",
				"C:/Shrt/SomeClass.g.cs",
				"C:/Some/Path/SomeClass.Properties.cs",
				"C:/Some/Longer/Path/SomeClass.cs",
			};

			Assert.AreEqual("C:/Some/Path/SomeClass.cs", SymbolExtensions.GetPreferredSymbolDeclaration(declarations));
		}

		[Test]
		public async Task Check_Metadata_Names()
		{
			using var workspace = WorkspaceUtils.GetSubjectSolution();

			await foreach (var (typeSymbol, compilation) in workspace.CurrentSolution.GetAllDeclaredTypes())
			{
				var synthesizedMetadataName = typeSymbol.GetFullMetadataName();
				var retrievedSymbol = compilation.GetTypeByMetadataName(synthesizedMetadataName);
				Assert.NotNull(retrievedSymbol);
				Assert.AreEqual(typeSymbol, retrievedSymbol);
			}
		}
	}
}
