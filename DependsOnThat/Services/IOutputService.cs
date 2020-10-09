using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Services
{
	/// <summary>
	/// Sends text to an output.
	/// </summary>
	internal interface IOutputService
	{
		/// <summary>
		/// Send a line of <paramref name="text"/> to the output.
		/// </summary>
		void WriteLine(string text);

		/// <summary>
		/// Focus the output or otherwise bring it into view (if applicable).
		/// </summary>
		void FocusOutput();
	}
}
