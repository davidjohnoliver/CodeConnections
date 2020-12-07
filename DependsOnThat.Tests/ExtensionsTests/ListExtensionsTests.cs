using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using NUnit.Framework;

namespace DependsOnThat.Tests.ExtensionsTests
{
	[TestFixture]
	public class ListExtensionsTests
	{
		[Test]
		public void When_GetUnorderedDiff_Unchanged()
		{
			var list1 = Enumerable.Range(0, 5).ToList();
			var list2 = Enumerable.Range(0, 5).ToList();

			var diffs = list2.GetUnorderedDiff(list1);
			;
			Assert.IsFalse(diffs.IsDifferent);
			CollectionAssert.IsEmpty(diffs.Added);
			CollectionAssert.IsEmpty(diffs.Removed);
		}

		[Test]
		public void When_GetUnorderedDiff_Added()
		{
			var list1 = Enumerable.Range(0, 5).ToList();
			var list2 = Enumerable.Range(0, 7).ToList();

			var diffs = list2.GetUnorderedDiff(list1);
			;
			Assert.IsTrue(diffs.IsDifferent);
			CollectionAssert.IsEmpty(diffs.Removed);
			Assert.IsTrue(new[] { 5, 6 }.SequenceEqual(diffs.Added));
		}

		[Test]
		public void When_GetUnorderedDiff_Removed()
		{
			var list1 = Enumerable.Range(0, 5).ToList();
			var list2 = Enumerable.Range(0, 3).ToList();

			var diffs = list2.GetUnorderedDiff(list1);
			;
			Assert.IsTrue(diffs.IsDifferent);
			CollectionAssert.IsEmpty(diffs.Added);
			Assert.IsTrue(new[] { 3, 4 }.SequenceEqual(diffs.Removed));
		}

		[Test]
		public void When_GetUnorderedDiff_Added_Removed()
		{
			var list1 = Enumerable.Range(0, 5).ToList();
			var list2 = new List<int> { 0, 2, 4, 8, 9 };

			var diffs = list2.GetUnorderedDiff(list1);
			;
			Assert.IsTrue(diffs.IsDifferent);
			Assert.IsTrue(new[] { 1, 3 }.SequenceEqual(diffs.Removed));
			Assert.IsTrue(new[] { 8, 9 }.SequenceEqual(diffs.Added));
		}
	}
}
