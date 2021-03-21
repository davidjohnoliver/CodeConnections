#nullable enable

using System.Windows.Controls;
using GraphSharp.Controls.Zoom;
using Xceed.Wpf.Toolkit;

namespace CodeConnections.Views
{
	public partial class DependencyGraphToolWindowControl : UserControl
	{
		public DependencyGraphToolWindowControl()
		{
			typeof(ZoomControl).ToString(); // Force an explicit dependency on GraphSharp here, so that assembly is resolved before parsing Xaml
			this.InitializeComponent();
		}
	}
}