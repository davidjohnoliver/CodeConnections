#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Extensions;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeConnections.Services
{
	internal partial class DocumentsService : IDocumentsService
	{
		private readonly DTE _dte;
		private readonly Func<IVsWindowFrame> _getToolWindowFrame;
		private string? _oldActiveDocument;

		public bool IsActiveDocumentInToolTabGroup => IsInToolWindowTabGroup(_activeDocumentFrame?.TargetOrDefault());

		public event EventHandler<ActiveDocumentChangedEventArgs>? ActiveDocumentChanged;

		public DocumentsService(DTE dte, Func<IVsWindowFrame> getToolWindowFrame)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			_dte = dte ?? throw new ArgumentNullException(nameof(dte));
			_getToolWindowFrame = getToolWindowFrame ?? throw new ArgumentNullException(nameof(getToolWindowFrame));
			_oldActiveDocument = GetActiveDocument();
		}
		public string? GetActiveDocument()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			try
			{
				return _dte.ActiveDocument?.FullName;
			}
			catch (Exception) // VS doesn't seem to like ActiveDocument being called when eg project settings are open
			{
				// TODO: log
				return null;
			}
		}

		public void OpenFileAsPreview(string fileName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			using (new NewDocumentStateScope(__VSNEWDOCUMENTSTATE.NDS_Provisional, VSConstants.NewDocumentStateReason.SolutionExplorer))
			{
				_dte.ItemOperations.OpenFile(fileName);
			}

		}
	}
}
