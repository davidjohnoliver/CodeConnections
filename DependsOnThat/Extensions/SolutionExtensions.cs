﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace DependsOnThat.Extensions
{
	public static class SolutionExtensions
	{
		/// <summary>
		/// Get <see cref="Document"/> corresponding to supplied file path.
		/// </summary>
		public static Document? GetDocument(this Solution solution, string filePath)
			=> solution.GetDocument(solution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault());
	}
}