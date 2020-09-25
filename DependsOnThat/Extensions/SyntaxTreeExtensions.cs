using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Roslyn.Utilities;

namespace DependsOnThat.Extensions
{
	public static class SyntaxTreeExtensions
	{
		/// <summary>
		/// Returns true if this <paramref name="syntaxTree"/> is obtained from generated code.
		/// </summary>
		public static bool IsGeneratedCode(this SyntaxTree syntaxTree)
		{
			// TODO: check explicit user definitions

			return GeneratedCodeUtilities.IsGeneratedCode(syntaxTree, SyntaxTriviaExtensions.IsRegularOrDocComment, cancellationToken: default);
		}
	}
}
