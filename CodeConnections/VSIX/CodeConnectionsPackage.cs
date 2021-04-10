#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CodeConnections.VSIX
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the
	/// IVsPackage interface and uses the registration attributes defined in the framework to
	/// register itself and its components with the shell. These attributes tell the pkgdef creation
	/// utility what data to put into .pkgdef file.
	/// </para>
	/// <para>
	/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
	/// </para>
	/// </remarks>
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[Guid(CodeConnectionsPackage.PackageGuidString)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[ProvideToolWindow(typeof(DependencyGraphToolWindow))]
	[ProvideOptionPage(typeof(UserOptionsDialog), "Code Connections", "General", 0, 0, true)]
	public sealed class CodeConnectionsPackage : AsyncPackage, IVsPersistSolutionOpts
	{
		/// <summary>
		/// CodeConnectionsPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "a2b91160-b751-4d85-967c-136fed47e2b2";

		internal static event Action? SaveUserOptions;

		internal static UserOptionsDialog? UserOptionsDialog { get; private set; }

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.
			await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			UserOptionsDialog = GetDialogPage(typeof(UserOptionsDialog)) as UserOptionsDialog;

			await DependencyGraphToolWindowCommand.InitializeAsync(this);

		}

		#endregion



		int IVsPersistSolutionOpts.SaveUserOptions(IVsSolutionPersistence pPersistence)
		{
			SaveUserOptions?.Invoke();
			return VSConstants.S_OK;
		}

		int IVsPersistSolutionOpts.LoadUserOptions(IVsSolutionPersistence pPersistence, uint grfLoadOpts) => VSConstants.S_OK;

		int IVsPersistSolutionOpts.WriteUserOptions(IStream pOptionsStream, string pszKey) => VSConstants.S_OK;

		int IVsPersistSolutionOpts.ReadUserOptions(IStream pOptionsStream, string pszKey) => VSConstants.S_OK;
	}
}
