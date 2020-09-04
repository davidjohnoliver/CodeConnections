#nullable enable

using System;
using System.Runtime.InteropServices;
using DependsOnThat.Disposables;
using DependsOnThat.Extensions;
using DependsOnThat.Presentation;
using DependsOnThat.Services;
using DependsOnThat.Views;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace DependsOnThat.VSIX
{
	/// <summary>
	/// This class implements the tool window exposed by this package and hosts a user control.
	/// </summary>
	/// <remarks>
	/// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
	/// usually implemented by the package implementer.
	/// <para>
	/// This class derives from the ToolWindowPane class provided from the MPF in order to use its
	/// implementation of the IVsUIElementPane interface.
	/// </para>
	/// </remarks>
	[Guid("a0c8e2a7-bd07-4d13-9e3c-855ecc9a027c")]
	public class DependencyGraphToolWindow : ToolWindowPane
	{
		private readonly CompositeDisposable _disposables = new CompositeDisposable();
		/// <summary>
		/// Initializes a new instance of the <see cref="DependencyGraphToolWindow"/> class.
		/// </summary>
		public DependencyGraphToolWindow() : base(null)
		{
			this.Caption = "DependsOnThat Graph";

			// This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
			// we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
			// the object returned by the Content property.
			this.Content = new DependencyGraphToolWindowControl();
		}

		protected override void Initialize()
		{
			base.Initialize();

			InitializeViewModel();
		}

		private void InitializeViewModel()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var dte = GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
			Assumes.Present(dte);
			var documentsService = new DocumentsService(dte);
			SubscribeListeners(documentsService);

			var componentModel = GetService(typeof(SComponentModel)) as IComponentModel;
			Assumes.Present(componentModel);
			var workspace = componentModel.GetService<VisualStudioWorkspace>();
			var roslynService = new RoslynService(workspace);

			var gitService = GitService.GetServiceOrDefault(dte.Solution.FullName);

			if (Content is DependencyGraphToolWindowControl content)
			{
				content.DataContext = new DependencyGraphToolWindowViewModel(ThreadHelper.JoinableTaskFactory, documentsService, roslynService, gitService)
					.DisposeWith(_disposables);
			}
		}

		private void SubscribeListeners(object service)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (service is IVsRunningDocTableEvents runningDocTableEvents)
			{
				var rdt = (IVsRunningDocumentTable)GetService(typeof(SVsRunningDocumentTable));
				Assumes.Present(rdt);
				rdt.AdviseRunningDocTableEvents(runningDocTableEvents, out var cookie);
				_disposables.Add(Disposable.Create(() =>
				{
					if (ThreadHelper.CheckAccess())
					{
						rdt.UnadviseRunningDocTableEvents(cookie);
					}
				}));
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (Content is DependencyGraphToolWindowControl content)
			{
				content.DataContext = null;
			}

			_disposables.Dispose();
		}
	}
}