using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;

namespace CodeConnections.Views
{
	public sealed partial class MermaidSummaryDialogWindow : DialogWindow
	{
		public MermaidSummaryDialogWindow()
		{
			this.InitializeComponent();
		}

		private void OnCloseButtonClicked(object sender, RoutedEventArgs e) => Close();
	}
}
