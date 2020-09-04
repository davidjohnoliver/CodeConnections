#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Disposables
{
	public static class Disposable
	{
		/// <summary>
		/// Creates a lightweight disposable.
		/// </summary>
		/// <param name="action">The action to be executed on disposal.</param>
		public static IDisposable Create(Action action) => new DisposableAction(action ?? throw new ArgumentNullException(nameof(action)));

		private class DisposableAction : IDisposable
		{
			private readonly Action _action;

			public DisposableAction(Action action)
			{
				_action = action;
			}

			public void Dispose() => _action.Invoke();
		}
	}
}
