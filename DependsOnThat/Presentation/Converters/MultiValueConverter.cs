#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using DependsOnThat.Utilities;

namespace DependsOnThat.Presentation.Converters
{
	/// <summary>
	/// An <see cref="IMultiValueConverter"/> with type-hinting, that expects two bound values
	/// </summary>
	/// <typeparam name="TValue1">The type of the value expected from the first binding</typeparam>
	/// <typeparam name="TValue2">The type of the value expected from the second binding</typeparam>
	/// <typeparam name="TTarget">The type being converted to.</typeparam>
	public abstract class MultiValueConverter<TValue1, TValue2, TTarget> : IMultiValueConverter
	{
		private const int ExpectedValues = 2;
		private static bool _isT1Nullable;
		private static bool _isT2Nullable;
		static MultiValueConverter()
		{
			_isT1Nullable = TypeUtils.IsRuntimeNullable<TValue1>();
			_isT2Nullable = TypeUtils.IsRuntimeNullable<TValue2>();
		}
		public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length == ExpectedValues)
			{
				var (t1, t1Valid) = Convert<TValue1>(values[0], _isT1Nullable);
				var (t2, t2Valid) = Convert<TValue2>(values[1], _isT2Nullable);

				if (t1Valid && t2Valid)
				{
					return ConvertInner(t1!, t2!, parameter, culture);
				}
			}

			return ConvertUnexpected(parameter, culture);
		}

		protected abstract TTarget ConvertInner(TValue1 value1, TValue2 value2, object parameter, CultureInfo culture);

		protected virtual TTarget ConvertUnexpected(object parameter, CultureInfo culture) => default!;

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException(); // TODO
		}

		private static (T? Value, bool IsValid) Convert<T>(object value, bool isTNullable)
		{
			if (value is T t)
			{
				return (t, true);
			}
			else if (isTNullable)
			{
				return (default, true);
			}
			else
			{
				return (default, false);
			}
		}
	}
}
