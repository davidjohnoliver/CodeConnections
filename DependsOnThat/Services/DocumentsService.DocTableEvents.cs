using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeConnections.Services
{
	partial class DocumentsService : IVsRunningDocTableEvents
	{
		public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

		public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

		public int OnAfterSave(uint docCookie) => VSConstants.S_OK;

		public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;

		public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var activeDocument = GetActiveDocument();
			if (activeDocument != _oldActiveDocument)
			{
				_oldActiveDocument = activeDocument;
				ActiveDocumentChanged?.Invoke();
			}
			return VSConstants.S_OK;
		}

		public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => VSConstants.S_OK;
	}
}
