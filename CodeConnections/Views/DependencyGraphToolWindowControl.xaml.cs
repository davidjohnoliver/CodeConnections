#nullable enable

using System.Windows.Controls;
using Xceed.Wpf.Toolkit;

namespace CodeConnections.Views
{
	public partial class DependencyGraphToolWindowControl : UserControl
	{
		public DependencyGraphToolWindowControl()
		{
			typeof(GraphSharp.Controls.Zoom.ZoomControl).ToString(); // Force an explicit dependency on GraphSharp here, so that assembly is resolved before parsing Xaml
			this.InitializeComponent();
		}
	}
}