#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Extensions
{
	public static class DisposableExtensions
	{
		/// <summary>
		/// Registers <paramref name="disposable"/> to be disposed with <paramref name="composite"/> (typically a <see cref="Disposables.CompositeDisposable"/>).
		/// </summary>
		/// <returns>The value of <paramref name="disposable"/>, for fluent usage</returns>
		public static T DisposeWith<T>(this T disposable, ICollection<IDisposable> composite) where T : class, IDisposable
		{
			if (disposable != null)
			{
				composite.Add(disposable);
			}

#pragma warning disable CS8603 // Possible null reference return. - The nullability of T (which we don't know) will be respected by the return since we're just returning the input value. NotNullIfNotNull doesn't seem to work.
			return disposable;
#pragma warning restore CS8603 // Possible null reference return.
		}
	}
}
