#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeConnections.Services
{
	internal class SolutionService : IVsSolutionEvents3, ISolutionService
	{
		private readonly DTE _dte;

		public event Action? SolutionOpened;
		public event Action? SolutionClosed;

		public SolutionService(EnvDTE.DTE dte)
		{
			_dte = dte;
		}

		public string GetSolutionPath()
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			return _dte.Solution.FullName;
		}

		#region IVsSolutionEvents3
		public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => VSConstants.S_OK;

		public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;

		public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;

		public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;

		public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;

		public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;

		public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
		{
			SolutionOpened?.Invoke();
			return VSConstants.S_OK;
		}

		public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;

		public int OnBeforeCloseSolution(object pUnkReserved) => VSConstants.S_OK;

		public int OnAfterCloseSolution(object pUnkReserved)
		{
			SolutionClosed?.Invoke();
			return VSConstants.S_OK;
		}

		public int OnAfterMergeSolution(object pUnkReserved) => VSConstants.S_OK;

		public int OnBeforeOpeningChildren(IVsHierarchy pHierarchy) => VSConstants.S_OK;

		public int OnAfterOpeningChildren(IVsHierarchy pHierarchy) => VSConstants.S_OK;

		public int OnBeforeClosingChildren(IVsHierarchy pHierarchy) => VSConstants.S_OK;

		public int OnAfterClosingChildren(IVsHierarchy pHierarchy) => VSConstants.S_OK;
		#endregion
	}
}
