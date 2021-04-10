#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.ComponentModel;
using CodeConnections.Presentation;
using Microsoft.VisualStudio.Shell;

namespace CodeConnections.VSIX
{
	public class UserOptionsDialog : DialogPage
	{
		private const string BasicOptionsString = "Basic Options";
		private const int BasicOptionsPosition = 0;
		private const string AdditionalOptionsString = "Additional Options";
		private const int AdditionalOptionsPosition = 1;
		private const int CategoryCount = 2;

		[SortedCategory(BasicOptionsString, BasicOptionsPosition, CategoryCount)]
		[DisplayName("Layout style")]
		[Description("Choose whether graph elements should be laid out in a vertical hierarchy, or in a compact space-efficient packing.")]
		public GraphLayoutMode LayoutMode { get; set; }

		[SortedCategory(BasicOptionsString, BasicOptionsPosition, CategoryCount)]
		[DisplayName("Always include active document")]
		[Description("Choose whether to always include the active document and its connections in the graph.")]
		public bool IsActiveAlwaysIncluded { get; set; } = true;


		[SortedCategory(AdditionalOptionsString, AdditionalOptionsPosition, CategoryCount)]
		[DisplayName("Element warning threshold")]
		[Description("The number of graph elements to load without warnings. If a graph operation would add more elements than this, a warning message appears.")]
		public int MaxAutomaticallyLoadedNodes { get; set; } = 100;

		internal event Action? OptionsApplied;

		protected override void OnApply(PageApplyEventArgs e)
		{
			base.OnApply(e);
			OptionsApplied?.Invoke();
		}
	}
}
