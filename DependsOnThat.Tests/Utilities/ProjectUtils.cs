#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DependsOnThat.Extensions;

namespace DependsOnThat.Tests.Utilities
{
	public class ProjectUtils
	{
		/// <summary>
		/// Returns all ProjectReference targets found in a .csproj file.
		/// </summary>
		/// <param name="csprojPath">Path to the file</param>
		public static IEnumerable<string> GetProjectReferences(string csprojPath)
		=> XDocument.Load(csprojPath)
			.Descendants(XName.Get("ProjectReference"))
			.Select(x => (x.Attribute(XName.Get("Include"))?.Value))
			.Trim();
	}
}
