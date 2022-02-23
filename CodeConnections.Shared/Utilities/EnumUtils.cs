#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Utilities
{
	public static class EnumUtils
	{
		/// <summary>
		/// Get all constant values of enum <typeparamref name="T"/>.
		/// </summary>
		public static T[] GetValues<T>() where T : Enum => Inner<T>.Values;

		/// <summary>
		/// Get all values of <typeparamref name="T"/> (which would typically be a Flags enum) that correspond to a single bit field set. This will
		/// not return the all-zero value, nor any integer values that are not actually defined in the <typeparamref name="T"/> enum.
		/// </summary>
		public static T[] GetSingleFieldValues<T>() where T : Enum => Inner<T>.SingleFieldValues;


		// TODO: thread safety
		private static class Inner<T> where T : Enum
		{
			private static T[]? _values;
			public static T[] Values => _values ??= Enum.GetValues(typeof(T)).Cast<T>().ToArray();

			private static HashSet<T>? _valuesSet;
			private static ISet<T> ValuesSet => _valuesSet ??= Values.ToHashSet();

			private static T[]? _singleFieldValues;
			public static T[] SingleFieldValues
			{
				get
				{
					if (_singleFieldValues == null)
					{
						var underlying = Enum.GetUnderlyingType(typeof(T));

						var values = new List<T>();
						switch (underlying)
						{
							case { } intType when intType == typeof(int):

								var current = 1;
								for (int i = 0; i < 32; i++)
								{
									var currentT = (T)(object)current;
									if (ValuesSet.Contains(currentT))
									{
										values.Add(currentT);
									}
									current = current << 1;
								}
								break;
							default:
								throw new NotSupportedException($"Support for {underlying} not added yet.");
						}

						_singleFieldValues = values.ToArray();
					}

					return _singleFieldValues;
				}
			}
		}
	}
}
