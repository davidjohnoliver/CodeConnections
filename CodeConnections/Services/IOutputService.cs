using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Presentation;

namespace CodeConnections.Services
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

		/// <summary>
		/// The current output verbosity level.
		/// </summary>
		OutputLevel CurrentOutputLevel { get; set; }

		/// <summary>
		/// Should messages at <paramref name="outputLevel"/> be sent to output?
		/// </summary>
		bool IsEnabled(OutputLevel outputLevel);
	}
}
