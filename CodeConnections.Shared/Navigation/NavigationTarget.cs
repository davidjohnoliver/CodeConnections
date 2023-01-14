#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace CodeConnections.Navigation
{
	/// <summary>
	/// A navigation destination.
	/// </summary>
	/// <typeparam name="TView">The view type to show.</typeparam>
	/// <typeparam name="TModel">The view model type.</typeparam>
	internal abstract class NavigationTarget<TView, TModel> where TView : FrameworkElement, new()
	{
		/// <summary>
		/// Get the view to be navigated to.
		/// </summary>
		internal virtual TView GetView() => new();

		/// <summary>
		/// Get the view model to be set on the target view.
		/// </summary>
		internal abstract TModel GetViewModel();
	}
}
