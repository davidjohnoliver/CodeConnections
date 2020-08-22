using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependsOnThat.Utilities
{
	/// <summary>
	/// Simple protection from freezing in while loops. Will throw an exception if the loop gets stuck.
	/// </summary>
	/// <remarks>
	/// Usage:
	/// var lp = new LoopProtection();
	///	while (true)
	///	{
	///		lp.Iterate();
	///		ManipulateState();
	///
	///		if (ComplexCondition()) {
	///			break;
	///		}
	///	}
	/// </remarks>
	public struct LoopProtection
	{
		private int _increments;
		private const int MaxIncrements = 10000000;

		/// <summary>
		/// Call on each iteration of the while loop.
		/// </summary>
		public void Iterate()
		{
			_increments++;
			if (_increments > MaxIncrements)
			{
				throw new InvalidOperationException($"Exceeded maximum loop iterations of {MaxIncrements}");
			}
		}
	}
}
