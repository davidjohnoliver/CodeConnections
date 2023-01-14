#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CodeConnections.Presentation
{
	internal sealed class MermaidSummaryDialogViewModel
	{
		private readonly string? _mermaidGraph;
		private readonly Exception? _error;
		private readonly bool _wasCopiedToClipboard;

		public string MainMessage => _mermaidGraph ?? _error?.ToString() ?? string.Empty;

		public string SecondaryMessage => GetSecondaryMessage();

		public MermaidSummaryDialogViewModel(string? mermaidGraph, Exception? error, bool wasCopiedToClipboard)
		{
			_mermaidGraph = mermaidGraph;
			_error = error;
			_wasCopiedToClipboard = wasCopiedToClipboard;
		}

		private string GetSecondaryMessage()
		{
			if (_wasCopiedToClipboard)
			{
				Debug.Assert(_mermaidGraph != null);
				Debug.Assert(_error == null);

				return "Copied to clipboard!";
			}

			if (_mermaidGraph != null)
			{
				return "Failed to copy to clipboard.";
			}

			return "Mermaid graph generation failed.";
		}
	}
}
