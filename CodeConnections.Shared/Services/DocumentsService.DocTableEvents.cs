#nullable enable

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
		private WeakReference<IVsWindowFrame>? _activeDocumentFrame;

		public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

		public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

		public int OnAfterSave(uint docCookie) => VSConstants.S_OK;

		public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;

		public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_activeDocumentFrame = new WeakReference<IVsWindowFrame>(pFrame);

			var activeDocument = GetActiveDocument();
			if (activeDocument != _oldActiveDocument)
			{
				_oldActiveDocument = activeDocument;
				ActiveDocumentChanged?.Invoke(this, ActiveDocumentChangedEventArgs.Empty);
			}
			return VSConstants.S_OK;
		}

		private bool IsInToolWindowTabGroup(IVsWindowFrame? frame)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (_getToolWindowFrame.Invoke() is IVsWindowFrame6 toolWindowFrame)
			{
				return toolWindowFrame.IsInSameTabGroup(frame);
			}

			return false;
		}

		public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => VSConstants.S_OK;
	}
}
