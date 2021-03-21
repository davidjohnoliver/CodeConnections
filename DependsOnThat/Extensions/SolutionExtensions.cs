#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeConnections.Utilities;
using Microsoft.CodeAnalysis;

namespace CodeConnections.Extensions
{
	public static class SolutionExtensions
	{
		/// <summary>
		/// Get <see cref="Document"/> corresponding to supplied file path.
		/// </summary>
		public static Document? GetDocument(this Solution solution, string filePath)
			=> solution.GetDocument(solution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault());

		public static IEnumerable<DocumentId> GetAllDocumentIds(this Solution solution) => solution.Projects.SelectMany(p => p.DocumentIds);
	}
}
