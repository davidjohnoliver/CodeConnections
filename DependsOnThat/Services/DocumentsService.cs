#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace DependsOnThat.Services
{
	internal class DocumentsService : IDocumentsService
	{
		private readonly DTE _dte;

		public DocumentsService(DTE dte)
		{
			_dte = dte ?? throw new ArgumentNullException(nameof(dte));
		}
		public string? GetActiveDocument()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			return _dte.ActiveDocument.FullName;
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
