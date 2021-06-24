#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Presentation;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeConnections.Services
{
	internal class OutputWindowOutputService : IOutputService
	{
		IVsOutputWindowPane? _outputPane;

		/// <summary>
		/// Have we already created (or tried and failed to create) the output pane?
		/// </summary>
		private bool _hasCreatedOutputPane;
		private readonly string _guidString;
		private readonly IVsOutputWindow _outputWindow;
		private readonly string _outputPaneName;

		public OutputLevel CurrentOutputLevel { get; set; }

		public OutputWindowOutputService(string guidString, IVsOutputWindow outputWindow, string outputPaneName)
		{
			_guidString = guidString;
			_outputWindow = outputWindow;
			_outputPaneName = outputPaneName;
		}

		private void TryCreateOutputPane()
		{
			if (_hasCreatedOutputPane)
			{
				return;
			}
			_hasCreatedOutputPane = true;

			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			var guid = new Guid(_guidString);
			_outputWindow.CreatePane(guid, _outputPaneName, 1, 1);
			_outputWindow.GetPane(guid, out _outputPane);
			if (_outputPane == null)
			{
				throw new InvalidOperationException("Failed to create output pane");
			}
		}

		public void WriteLine(string output)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			TryCreateOutputPane();

			_outputPane?.OutputString($"{output}{Environment.NewLine}");
		}

		public void FocusOutput()
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			TryCreateOutputPane();
			_outputPane?.Activate();
		}

		public bool IsEnabled(OutputLevel outputLevel) => outputLevel <= CurrentOutputLevel;
	}
}
