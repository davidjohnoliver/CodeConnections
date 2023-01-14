#nullable enable

using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeConnections.Navigation
{
	internal class NavigationService : INavigationService
	{
		public void ShowModal<TView, TModel>(ModalNavigationTarget<TView, TModel> modalNavigationTarget) where TView : DialogWindow, new()
		{
			var dialogWindow = modalNavigationTarget.GetView();
			var vm = modalNavigationTarget.GetViewModel();

			dialogWindow.DataContext = vm;

			dialogWindow.ShowModal();
		}
	}
}
