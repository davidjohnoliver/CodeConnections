#nullable enable

using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeConnections.Navigation;

/// <summary>
/// Provides methods for user navigation.
/// </summary>
internal interface INavigationService
{
	/// <summary>
	/// Show a modal window.
	/// </summary>
	/// <param name="modalNavigationTarget">The modal navigation target to show.</param>
	void ShowModal<TView, TModel>(ModalNavigationTarget<TView, TModel> modalNavigationTarget) where TView : DialogWindow, new();
}