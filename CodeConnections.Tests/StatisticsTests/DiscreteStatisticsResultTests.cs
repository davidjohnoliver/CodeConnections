﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Statistics;
using NUnit.Framework;

namespace CodeConnections.Tests.StatisticsTests
{
	[TestFixture]
	public class DiscreteStatisticsResultTests
	{
		[Test]
		public void When_Simple()
		{
			var rawValues = new[] { 12, 1, 5, 7, 5, 8, -4, 5, 1, 22, 5, 1, 8 };
			var values = rawValues.Select(i => new SimpleWrapper(i)).ToArray();

			var mean = (double)rawValues.Sum() / rawValues.Count();
			var min = -4;
			var max = 22;
			var mode = 5;
			var sortedDistinct = rawValues.Distinct().OrderBy(i => i).ToArray();

			var statisticsResult = DiscreteStatisticsResult.Create(values, v => v.Value);

			Assert.AreEqual(min, statisticsResult.Min);
			Assert.AreEqual(max, statisticsResult.Max);
			Assert.AreEqual(mode, statisticsResult.Mode);
			Assert.AreEqual(mean, statisticsResult.Mean);

			var index = -1;
			foreach (var bucket in statisticsResult.BucketValues)
			{
				index++;
				Assert.AreEqual(sortedDistinct[index], bucket);
			}

			Assert.AreEqual(4, statisticsResult.Histogram[5]);
			Assert.AreEqual(0, statisticsResult.Histogram[6]);
			Assert.IsFalse(statisticsResult.Histogram.ContainsKey(23));
			Assert.IsTrue(statisticsResult.Histogram.ContainsKey(20));
		}

		[Test]
		public void When_Bucket_Counts()
		{
			var rawValues = new[] { 11, 11, 11, 12, 14, 14 };
			var values = rawValues.Select(i => new SimpleWrapper(i)).ToArray();

			var statisticsResult = DiscreteStatisticsResult.Create(values, v => v.Value);

			Assert.AreEqual(4, statisticsResult.Histogram.Count);

			Assert.AreEqual(3, statisticsResult.MaxBucketCount);
			Assert.AreEqual(0, statisticsResult.MinBucketCount);
			Assert.AreEqual(1.5, statisticsResult.MeanBucketCount);
		}

		[Test]
		public void When_Simple_BucketSD()
		{
			var rawValues = new[] { 11, 11, 11, 12, 14, 14 };
			var values = rawValues.Select(i => new SimpleWrapper(i)).ToArray();

			const double expectedSD = 1.1180339887499; // https://www.calculator.net/standard-deviation-calculator.html?numberinputs=3%2C+1%2C+0%2C+2&ctype=p&x=48&y=24

			var statisticsResult = DiscreteStatisticsResult.Create(values, v => v.Value);

			Assert.AreEqual(expectedSD, statisticsResult.SDBucketCount, delta: 1e-6);
		}

		public class SimpleWrapper
		{
			public SimpleWrapper(int value)
			{
				Value = value;
			}

			public int Value { get; }
		}
	}
}
