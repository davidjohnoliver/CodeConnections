#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Presentation
{
	public abstract class ViewModelBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Call from a bindable property to raise the <see cref="PropertyChanged"/> event when needed.
		/// </summary>
		/// <param name="currentValue">Pass the backing field here.</param>
		/// <param name="newValue">Pass the set value here.</param>
		/// <returns>True if the value changed, false if the new value is equal to the previous.</returns>
		protected bool OnValueSet<T>(ref T currentValue, T newValue, [CallerMemberName] string? name = null)
		{
			if (!Equals(currentValue, newValue))
			{
				currentValue = newValue;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
				return true;
			}

			return false;
		}
	}
}
