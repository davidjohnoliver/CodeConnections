using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Text
{
	/// <summary>
	/// Formats a sequence of values as a single string.
	/// </summary>
	public interface ICompactListFormatter
	{
		string FormatList<T>(IEnumerable<T> list);
	}
}
