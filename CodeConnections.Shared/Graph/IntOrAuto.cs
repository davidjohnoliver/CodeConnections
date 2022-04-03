#nullable enable

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeConnections.Graph
{
	public struct IntOrAuto
	{
		[JsonProperty]
		public int Value { get; }

		[JsonProperty]
		public bool IsAuto { get; private init; }

		public IntOrAuto(int value)
		{
			Value = value;
			IsAuto = false;
		}

		public static explicit operator IntOrAuto(int value) => new IntOrAuto(value);

		public static IntOrAuto Auto { get; } = new(int.MinValue) { IsAuto = true };

		public override string ToString()
		{
			return IsAuto ? "Auto" : Value.ToString();
		}

		public override bool Equals(object obj)
		{
			if (obj is IntOrAuto other)
			{
				var value1 = this;
				return Equals(other, value1);
			}
			return false;
		}

		public override int GetHashCode() => IsAuto.GetHashCode() * 31 + Value.GetHashCode();

		public static bool operator ==(IntOrAuto value1, IntOrAuto value2) => Equals(value1, value2);
		public static bool operator !=(IntOrAuto value1, IntOrAuto value2) => !Equals(value1, value2);

		private static bool Equals(IntOrAuto value1, IntOrAuto value2)
			=> value1.IsAuto == value2.IsAuto && value1.Value == value2.Value;
	}
}
