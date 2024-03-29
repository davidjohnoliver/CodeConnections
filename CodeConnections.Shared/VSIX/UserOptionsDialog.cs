﻿#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.ComponentModel;
using CodeConnections.Graph;
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
		[Description("Choose whether to always include the active document in the graph.")]
		public bool IsActiveAlwaysIncluded { get; set; } = true;

		[SortedCategory(BasicOptionsString, BasicOptionsPosition, CategoryCount)]
		[DisplayName("How to include active")]
		[Description("Choose whether to include the active document only, or its connections as well.")]
		public IncludeActiveMode IncludeActiveMode { get; set; } = IncludeActiveMode.ActiveAndConnections;


		[SortedCategory(AdditionalOptionsString, AdditionalOptionsPosition, CategoryCount)]
		[DisplayName("Element warning threshold")]
		[Description("The number of graph elements to load without warnings. If a graph operation would add more elements than this, a warning message appears.")]
		public int MaxAutomaticallyLoadedNodes { get; set; } = 100;


		[SortedCategory(AdditionalOptionsString, AdditionalOptionsPosition, CategoryCount)]
		[DisplayName("Output verbosity")]
		[Description("Choose the volume of messages to output to the console.")]
		public OutputLevel OutputLevel { get; set; } = OutputLevel.Normal;


		[SortedCategory(AdditionalOptionsString, AdditionalOptionsPosition, CategoryCount)]
		[DisplayName("Enable debug tools")]
		[Description("Enables additional features that are primarily useful in debugging the extension.")]
		public bool EnableDebugFeatures { get; set; } = false;

		internal event Action? OptionsApplied;

		protected override void OnApply(PageApplyEventArgs e)
		{
			base.OnApply(e);
			OptionsApplied?.Invoke();
		}
	}
}
