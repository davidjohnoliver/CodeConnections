#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace DependsOnThat.Views
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
		/// <summary>
		/// Initializes a new instance of the <see cref="DependencyGraphToolWindow"/> class.
		/// </summary>
		public DependencyGraphToolWindow() : base(null)
		{
			this.Caption = "DependencyGraphToolWindow";

			// This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
			// we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
			// the object returned by the Content property.
			this.Content = new DependencyGraphToolWindowControl();
		}
	}
}
