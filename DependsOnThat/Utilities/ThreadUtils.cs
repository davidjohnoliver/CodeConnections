#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DependsOnThat.Utilities
{
	public static class ThreadUtils
	{
		/// <summary>
		/// A wrapper for <see cref="ThreadHelper.ThrowIfNotOnUIThread(string)"/> which doesn't need to be duplicated ad nauseam to satisfy analyzers.
		/// </summary>
		public static void ThrowIfNotOnUIThread()
		{
			Action throwMethod = ThrowInner; // Throwing VSTHRD010 off the trail
			throwMethod();
		}

		private static void ThrowInner()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
		}

		public static async Task RunOnUIThread(Action callback, CancellationToken ct)
		{
			if (callback is null)
			{
				throw new ArgumentNullException(nameof(callback));
			}

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
			callback();
		}
	}
}
