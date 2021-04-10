#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace CodeConnections.VSIX
{
	public class UserOptionsDialog : DialogPage
	{
		[Category("Graph Options")]
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
