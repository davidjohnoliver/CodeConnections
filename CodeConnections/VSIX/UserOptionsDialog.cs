#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Presentation;
using Microsoft.VisualStudio.Shell;

namespace CodeConnections.VSIX
{
	public class UserOptionsDialog : DialogPage
	{
		private const string GraphOptionsString = "Graph Options";

		[Category(GraphOptionsString)]
		[DisplayName("Element warning threshold")]
		[Description("The number of graph elements to load without warnings. If a graph operation would add more elements than this, a warning message appears.")]
		public int MaxAutomaticallyLoadedNodes { get; set; } = 100;

		[Category(GraphOptionsString)]
		[DisplayName("Layout style")]
		[Description("Choose whether graph elements should be laid out in a vertical hierarchy, or in a compact space-efficient packing.")]
		public GraphLayoutMode LayoutMode { get; set; }

		[Category(GraphOptionsString)]
		[DisplayName("Always include active document")]
		[Description("Choose whether to always include the active document and its connections in the graph.")]
		public bool IsActiveAlwaysIncluded { get; set; } = true;

		internal event Action? OptionsApplied;

		protected override void OnApply(PageApplyEventArgs e)
		{
			base.OnApply(e);
			OptionsApplied?.Invoke();
		}
	}
}
