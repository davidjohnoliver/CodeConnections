using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Text
{
	/// <summary>
	/// Formats header text.
	/// </summary>
	public interface IHeaderFormatter
	{
		/// <summary>
		/// Returns formatted header.
		/// </summary>
		/// <param name="headerContent">The header content</param>
		/// <param name="headerLevel">The header level (think h1, h2 etc)</param>
		string FormatHeader(string headerContent, int headerLevel);
	}
}
