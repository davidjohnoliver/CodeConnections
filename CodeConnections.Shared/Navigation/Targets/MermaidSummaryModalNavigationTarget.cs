#nullable enable

using CodeConnections.Presentation;
using CodeConnections.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeConnections.Navigation.Targets
{
	internal class MermaidSummaryModalNavigationTarget : ModalNavigationTarget<MermaidSummaryDialogWindow, MermaidSummaryDialogViewModel>
	{
		public required MermaidSummaryDialogViewModel ViewModel { get; init; }

		internal override MermaidSummaryDialogViewModel GetViewModel()
		{
			return ViewModel;
		}
	}
}
