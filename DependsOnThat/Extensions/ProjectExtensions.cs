#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Roslyn;
using Microsoft.CodeAnalysis;

namespace CodeConnections.Extensions
{
	public static class ProjectExtensions
	{
		/// <summary>
		/// Gets a lightweight identifier for <paramref name="project"/>.
		public static ProjectIdentifier ToIdentifier(this Project project) => new ProjectIdentifier(project.Id, project.Name);
	}
}
