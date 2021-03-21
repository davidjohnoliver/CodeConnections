#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CodeConnections.Disposables;
using CodeConnections.Extensions;
using CodeConnections.Presentation;
using CodeConnections.Services;
using CodeConnections.Views;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeConnections.VSIX
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
		private const string ConsolePaneId = "64793CEA-04A1-449A-8F30-6A3EE7581BAB";

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

			ThreadHelper.ThrowIfNotOnUIThread();

			InitializeViewModel();
		}

		private void InitializeViewModel()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var dte = GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
			AssumePresent(dte);
			var documentsService = new DocumentsService(dte);
			SubscribeListeners(documentsService);

			var componentModel = GetService(typeof(SComponentModel)) as IComponentModel;
			AssumePresent(componentModel);
			var workspace = componentModel.GetService<VisualStudioWorkspace>();
			var roslynService = new RoslynService(workspace).DisposeWith(_disposables);

			var solutionService = new SolutionService(dte);
			SubscribeListeners(solutionService);

			var outputWindow = GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
			AssumePresent(outputWindow);

			var outputService = new OutputWindowOutputService(ConsolePaneId, outputWindow, "DependsOnThat");

			var gitService = new GitService(solutionService).DisposeWith(_disposables);

			var solutionPersistence = (IVsSolutionPersistence)GetService(typeof(SVsSolutionPersistence));
			var settingsService = new SettingsService(solutionPersistence);

			if (Content is DependencyGraphToolWindowControl content)
			{
				content.DataContext = new DependencyGraphToolWindowViewModel(ThreadHelper.JoinableTaskFactory, documentsService, roslynService, gitService, solutionService, outputService, roslynService, settingsService)
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
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
				_disposables.Add(Disposable.Create(() =>
				{
					if (ThreadHelper.CheckAccess())
					{
						rdt.UnadviseRunningDocTableEvents(cookie);
					}
				}));
			}

			if (service is IVsSolutionEvents solutionEvents)
			{
				var solution = GetService(typeof(SVsSolution)) as IVsSolution;
				AssumePresent(solution);
				solution.AdviseSolutionEvents(solutionEvents, out var cookie);
				_disposables.Add(Disposable.Create(() =>
				{
					if (ThreadHelper.CheckAccess())
					{
						solution.UnadviseSolutionEvents(cookie);
					}
				}));
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
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

		private static void AssumePresent<T>([NotNull] T? component) where T : class
		{
			Assumes.Present(component);
#pragma warning disable CS8777 // Parameter must have a non-null value when exiting. - Assumes.Present() will throw if null
		}
#pragma warning restore CS8777 // Parameter must have a non-null value when exiting.
	}
}