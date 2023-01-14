#nullable enable

using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeConnections.Navigation
{
	/// <summary>
	/// A modal window navigation destination.
	/// </summary>
	/// <typeparam name="TView">The view type to show.</typeparam>
	/// <typeparam name="TModel">The view model type.</typeparam>
	internal abstract class ModalNavigationTarget<TView, TModel> : NavigationTarget<TView, TModel> where TView : DialogWindow, new()
	{
	}
}
