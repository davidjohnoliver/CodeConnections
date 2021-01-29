using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Utilities
{
	public static class TypeUtils
	{
		/// <summary>
		/// Is <typeparamref name="T"/> a reference type or a <see cref="Nullable{T}"/>?
		/// </summary>
		/// <remarks>
		/// This ignores nullability annotations on reference types, ie it will return 'true' for non-nullable reference types.
		/// </remarks>
		public static bool IsRuntimeNullable<T>()
		{
			var type = typeof(T);
			if (!type.IsValueType)
			{
				return true;
			}
			if (Nullable.GetUnderlyingType(type) != null)
			{
				return true;
			}
			return false;
		}
	}
}
