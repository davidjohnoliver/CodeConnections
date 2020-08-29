#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DependsOnThat.Presentation.Converters
{
	/// <summary>
	/// An <see cref="IValueConverter"/> with type-hinting
	/// </summary>
	/// <typeparam name="TValue">The type of the value expected from the binding.</typeparam>
	/// <typeparam name="TTarget">The type being converted to.</typeparam>
	public abstract class ValueConverter<TValue, TTarget> : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) => value switch
		{
			TValue tValue => ConvertInner(tValue, parameter, culture),
			null => ConvertNull(parameter, culture),
			_ => ConvertIncorrectType(parameter, culture)
		};

		protected abstract TTarget ConvertInner(TValue value, object parameter, CultureInfo culture);
		protected abstract TTarget ConvertNull(object parameter, CultureInfo culture);
		protected virtual object? ConvertIncorrectType(object parameter, CultureInfo culture) => null;

		public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture) => value switch
		{
			TTarget tValue => ConvertBackInner(tValue, parameter, culture),
			null => ConvertBackNull(parameter, culture),
			_ => ConvertBackIncorrectType(parameter, culture)
		};

		protected virtual TValue ConvertBackInner(TTarget value, object parameter, CultureInfo culture) => throw new NotSupportedException();
		protected virtual TValue ConvertBackNull(object parameter, CultureInfo culture) => throw new NotSupportedException();
		protected virtual object ConvertBackIncorrectType(object parameter, CultureInfo culture) => throw new NotSupportedException();
	}
}
