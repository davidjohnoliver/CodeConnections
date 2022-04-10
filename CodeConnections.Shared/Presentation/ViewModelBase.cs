#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Extensions;

namespace CodeConnections.Presentation
{
	public abstract class ViewModelBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		private HashSet<string>? _registeredComposedProperties;

		private Dictionary<string, List<Action>>? _propertyCallbacks;

		/// <summary>
		/// Used as an optimization to avoid raising PropertyChanged for <see cref="Self"/> if it's unused.
		/// </summary>
		private bool _isSelfPropertyConsumed;

		/// <summary>
		/// A binding-friendly reference to the instance itself, for which <see cref="PropertyChanged"/> will be raised when any known properties change.
		/// </summary>
		public ViewModelBase Self
		{
			get
			{
				_isSelfPropertyConsumed = true;
				return this;
			}
		}

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
				OnPropertyChanged(name);
				return true;
			}

			return false;
		}

		protected bool OnValueSet<T>(T currentValue, Action<T> setter, T newValue, [CallerMemberName] string? name = null)
		{
			if (!Equals(currentValue, newValue))
			{
				setter(newValue);
				OnPropertyChanged(name);
				return true;
			}

			return false;
		}

		/// <summary>
		/// When <paramref name="name"/> changes, raise <see cref="PropertyChanged"/> and call any internal callbacks.
		/// </summary>
		private void OnPropertyChanged(string? name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

			if (name is not null && _propertyCallbacks?.GetOrDefault(name) is { } callbackList)
			{
				foreach (var callback in callbackList)
				{
					callback?.Invoke();
				}
			}

			if (_isSelfPropertyConsumed)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Self)));
			}
		}

		/// <summary>
		/// Gets the value of a property, composed from <paramref name="firstInput"/> and <paramref name="secondInput"/> and calculated
		/// from <paramref name="selector"/>. Once this has been called for a given composed property, the <see cref="PropertyChanged"/> event
		/// will be raised for the composed property if the <paramref name="firstInputName"/> or <paramref name="secondInputName"/> properties change.
		/// </summary>
		/// <returns>The value of the calculated property.</returns>
		/// <remarks>
		/// The property changed event won't be raised for this property until after it's been accessed at least once. We rely on the fact that
		/// data-binding consumers typically access a property immediately when they become interested in updates to it.
		/// </remarks>
		protected TValue Compose<TInput1, TInput2, TValue>(
			TInput1 firstInput,
			string firstInputName,
			TInput2 secondInput,
			string secondInputName,
			Func<TInput1, TInput2, TValue> selector,
			[CallerMemberName] string? name = null
		)
		{
			if (name is null)
			{
				throw new ArgumentNullException(nameof(name));
			}

			if (!(_registeredComposedProperties?.Contains(name) ?? false))
			{

				(_registeredComposedProperties ??= new()).Add(name);
				RegisterPropertyCallback(firstInputName, Callback);
				RegisterPropertyCallback(secondInputName, Callback);

				void Callback() => OnPropertyChanged(name);
			}

			return selector(firstInput, secondInput);
		}

		private void RegisterPropertyCallback(string property, Action callback) =>
			(_propertyCallbacks ??= new())
				.GetOrCreate(property, _ => new())
				.Add(callback);
	}
}
