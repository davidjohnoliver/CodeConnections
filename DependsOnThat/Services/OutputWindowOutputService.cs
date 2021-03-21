#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeConnections.Services
{
	internal class OutputWindowOutputService : IOutputService
	{
		IVsOutputWindowPane _outputPane;

		public OutputWindowOutputService(string guidString, IVsOutputWindow outputWindow, string outputPaneName)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			var guid = new Guid(guidString);
			outputWindow.CreatePane(guid, outputPaneName, 1, 1);
			outputWindow.GetPane(guid, out _outputPane);
			if (_outputPane == null)
			{
				throw new InvalidOperationException("Failed to create output pane");
			}
		}

		public void WriteLine(string output)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			_outputPane.OutputString($"{output}{Environment.NewLine}");
		}

		public void FocusOutput()
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			_outputPane.Activate();
		}
	}
}
