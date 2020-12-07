#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DependsOnThat.Utilities
{
	/// <summary>
	/// Exposes an awaitable which completes after an 'idle' state has been reached, ie after no 'activity' has occurred (where activity is 
	/// defined and reported by the consumer). It can also be 'fast-tracked' to immediate completion while waiting for idle.
	/// </summary>
	/// <remarks>Not thread-safe.</remarks>
	public sealed class IdleTimer
	{
		private readonly int _idleWaitTimeMS;

		private readonly Stopwatch _activityStopwatch = new Stopwatch();

		private TaskCompletionSource<bool>? _taskCompletionSource;

		/// <summary>
		/// Construct a new <see cref="IdleTimer"/>.
		/// </summary>
		/// <param name="idleWaitTimeMS">
		/// The minimum amount of time before idle state is considered to be reached. The actual wait time 
		/// might be longer than this, but will not be shorter.
		/// </param>
		public IdleTimer(int idleWaitTimeMS)
		{
			_idleWaitTimeMS = idleWaitTimeMS;

			RecordActive();
		}

		/// <summary>
		/// Record 'activity', resetting the timer for idle state to be reached.
		/// </summary>
		public void RecordActive() => _activityStopwatch.Restart();

		/// <summary>
		/// Returns an awaitable that will complete either:
		///  - after the idle state is reached
		///  - when <paramref name="ct"/> is cancelled
		///  - when <see cref="FastTrackTimer"/> is called
		/// </summary>
		public async Task WaitForIdle(CancellationToken ct)
		{
			var tcs = new TaskCompletionSource<bool>();

			ct.Register(() => tcs.TrySetResult(Dummy));

			async Task WaitTask()
			{
				while (_activityStopwatch.ElapsedMilliseconds < _idleWaitTimeMS && !ct.IsCancellationRequested)
				{
					await Task.Delay(_idleWaitTimeMS / 4);
				}
				tcs.TrySetResult(Dummy);
			}

			_taskCompletionSource = tcs;

			await Task.WhenAny(WaitTask(), tcs.Task);
		}

		/// <summary>
		/// Accelerate the awaitable last returned by <see cref="WaitForIdle(CancellationToken)"/> to immediate completion.
		/// </summary>
		public void FastTrackTimer() => _taskCompletionSource?.TrySetResult(Dummy);

		private const bool Dummy = true;
	}
}
