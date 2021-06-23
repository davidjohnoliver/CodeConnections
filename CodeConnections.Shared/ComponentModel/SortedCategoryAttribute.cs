using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.ComponentModel
{
	/// <summary>
	/// Extends <see cref="CategoryAttribute"/> with sort order.
	/// </summary>
	// https://stackoverflow.com/a/21441892/1902058
	public class SortedCategoryAttribute : CategoryAttribute
	{
		private const char NonPrintableChar = '\t';

		/// <summary>
		/// Initialize a new instance of <see cref="SortedCategoryAttribute"/>.
		/// </summary>
		/// <param name="category">The category name.</param>
		/// <param name="categoryPosition">The sort position of this category, 0-based.</param>
		/// <param name="totalCategories">The total number of categories.</param>
		public SortedCategoryAttribute(string category,
												ushort categoryPosition,
												ushort totalCategories)
			: base(category.PadLeft(category.Length + (totalCategories - categoryPosition - 1),
						SortedCategoryAttribute.NonPrintableChar))
		{
		}
	}
}
