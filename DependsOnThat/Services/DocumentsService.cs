#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

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
	}
}
