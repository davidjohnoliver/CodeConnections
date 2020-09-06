using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DependsOnThat.Disposables
{
	/// <summary>
	/// A combination of a <see cref="SerialDisposable"/> and a <see cref="CancellationDisposable"/>.
	/// </summary>
	public class SerialCancellationDisposable : IDisposable
	{
		private readonly SerialDisposable _inner = new SerialDisposable();

		/// <summary>
		/// Cancels the current token and returns a new one.
		/// </summary>
		public CancellationToken GetNewToken()
		{
			var cd = new CancellationDisposable();
			_inner.Disposable = cd;
			return cd.Token;
		}

		/// <summary>
		/// Cancels the current token.
		/// </summary>
		public void Cancel() => _inner.Disposable = null;

		public void Dispose()
		{
			_inner.Dispose();
		}
	}
}
