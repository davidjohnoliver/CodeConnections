#nullable enable

using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Presentation;
using DependsOnThat.Services;
using DependsOnThat.Views;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace DependsOnThat.VSIX
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class DependencyGraphToolWindowCommand
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0100;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("457e67ce-4bad-41e5-b06f-aa298a6e5599");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private readonly AsyncPackage package;

		/// <summary>
		/// Initializes a new instance of the <see cref="DependencyGraphToolWindowCommand"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		/// <param name="commandService">Command service to add command to, not null.</param>
		private DependencyGraphToolWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
		{
			this.package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new MenuCommand(this.Execute, menuCommandID);
			commandService.AddCommand(menuItem);
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static DependencyGraphToolWindowCommand? Instance
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the service provider from the owner package.
		/// </summary>
		private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
		{
			get
			{
				return this.package;
			}
		}

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public static async Task InitializeAsync(AsyncPackage package)
		{
			// Switch to the main thread - the call to AddCommand in DependencyGraphToolWindowCommand's constructor requires
			// the UI thread.
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService ?? throw new InvalidOperationException("Command service not found");
			Instance = new DependencyGraphToolWindowCommand(package, commandService);
		}

		/// <summary>
		/// Shows the tool window when the menu item is clicked.
		/// </summary>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event args.</param>
		private void Execute(object sender, EventArgs e)
		{
			this.package.JoinableTaskFactory.RunAsync(async delegate
			{
				var window = await this.package.ShowToolWindowAsync(typeof(DependencyGraphToolWindow), 0, true, this.package.DisposalToken);
				if ((null == window) || (null == window.Frame))
				{
					throw new NotSupportedException("Cannot create tool window");
				}
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				var dte = await ServiceProvider.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
				Assumes.Present(dte);
				var documentsService = new DocumentsService(dte);
				var componentModel = await ServiceProvider.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
				Assumes.Present(componentModel);
				var workspace = componentModel.GetService<VisualStudioWorkspace>();
				var roslynService = new RoslynService(workspace);
				var gitService = GitService.GetServiceOrDefault(dte.Solution.FullName);
				if (window.Content is DependencyGraphToolWindowControl content)
				{
					content.DataContext = new DependencyGraphToolWindowViewModel(package.JoinableTaskFactory, documentsService, roslynService, gitService);
				}
			});
		}
	}
}
