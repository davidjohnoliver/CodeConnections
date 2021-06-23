using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Text
{
	/// <summary>
	/// Formats a dictionary of key-value pairs as a sequence of strings.
	/// </summary>
	public interface IDictionaryFormatter
	{
		IEnumerable<string> FormatDictionary<TKey, TValue>(string keyHeader, string valueHeader, IEnumerable<KeyValuePair<TKey, TValue>> dictionary);
	}
}
