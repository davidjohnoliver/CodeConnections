using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace CodeConnections.Tests.Utilities
{
	partial class WorkspaceUtils
	{
		/// <summary>
		/// Gets a <see cref="Workspace"/> associated with the test solution.
		/// </summary>
		/// <returns></returns>
		public static Workspace GetSubjectSolution()
		{
			var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			const string subjectPath = @"../../SubjectSolution";
			return GetWorkspace(Path.Combine(location, subjectPath));
		}
	}
}
