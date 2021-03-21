#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Collections;

namespace CodeConnections.Extensions
{
	public static class SelectionListExtensions
	{
		public static IEnumerable<T> UnselectedItems<T>(this SelectionList<T> selectionList) => selectionList.Except(selectionList.SelectedItems);
	}
}
