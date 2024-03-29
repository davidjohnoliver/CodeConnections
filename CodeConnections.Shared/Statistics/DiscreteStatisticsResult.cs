﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Extensions;
using QuickGraph.Algorithms;

namespace CodeConnections.Statistics
{
	/// <summary>
	/// Contains statistical information about a discrete distribution.
	/// </summary>
	/// <typeparam name="T">Item type</typeparam>
	public class DiscreteStatisticsResult<T>
	{
		/// <summary>
		/// Items 'bucketed' by their corresponding values in the distribution. Ordered. Sparse.
		/// </summary>
		public IReadOnlyDictionary<int, IReadOnlyList<T>> ItemsByBucket { get; }

		/// <summary>
		/// A histogram of bucket counts keyed by bucket value. Ordered. Dense.
		/// </summary>
		public IReadOnlyDictionary<int, int> Histogram { get; }

		/// <summary>
		/// A histogram of bucket counts keyed by bucket value. Ordered. Sparse.
		/// </summary>
		public IEnumerable<KeyValuePair<int, int>> SparseHistogram => Histogram.Where(kvp => kvp.Value > 0);

		/// <summary>
		/// All bucket values, ie all distribution values which are represented by at least one item. Ordered. Sparse.
		/// </summary>
		public IReadOnlyList<int> BucketValues { get; }

		/// <summary>
		/// The most commonly occurring value in the sample.
		/// </summary>
		public int Mode { get; }

		/// <summary>
		/// The mean sample value.
		/// </summary>
		public double Mean { get; }

		/// <summary>
		/// Total items in the sample.
		/// </summary>
		public int ItemsCount { get; }

		/// <summary>
		/// Minimum value in the sample.
		/// </summary>
		public int Min => BucketValues[0];

		/// <summary>
		/// Maximum sample value.
		/// </summary>
		public int Max => BucketValues[^1];

		/// <summary>
		/// The number of items in the bucket with least items. This is taken from the dense bucket set between min and max values, and therefore may be 0.
		/// </summary>
		public int MinBucketCount { get; }

		/// <summary>
		/// The number of items in the bucket with most items.
		/// </summary>
		public int MaxBucketCount { get; }

		/// <summary>
		/// The sample value corresponding to the bucket with most items.
		/// </summary>
		public int MaxBucketCountBucket => SparseHistogram.First(kvp => kvp.Value == MaxBucketCount).Key; // TODO: this is horribly inefficient

		/// <summary>
		/// The mean number of items per bucket. This is calculated over the dense bucket set between min and max values.
		/// </summary>
		public double MeanBucketCount { get; }

		/// <summary>
		/// The standard deviation in the number of items per bucket (calculated over dense bucket set).
		/// </summary>
		public double SDBucketCount { get; }

		private DiscreteStatisticsResult(bool isEmpty)
		{
			if (!isEmpty)
			{
				throw new ArgumentException("Use ctor that takes a sample.");
			}

			ItemsByBucket = new Dictionary<int, IReadOnlyList<T>>();
			Histogram = new Dictionary<int, int>();
			BucketValues = new List<int>();
		}

		/// <param name="sample">Set of items constituting the sample</param>
		/// <param name="valueSelector">Selector to determine the value in the distribution for each item</param>
		public DiscreteStatisticsResult(IEnumerable<T> sample, Func<T, int> valueSelector)
		{
			if (!sample.Any())
			{
				throw new ArgumentException($"{nameof(sample)} contains no items.");
			}
			if (valueSelector is null)
			{
				throw new ArgumentNullException(nameof(valueSelector));
			}

			var histogram = new Dictionary<int, int>();
			Histogram = histogram;
			var itemsByBucket = new Dictionary<int, IReadOnlyList<T>>();
			ItemsByBucket = itemsByBucket;
			var buckets = new List<int>();
			BucketValues = buckets;

			// Save buckets as we find them; the stored dictionary will be populated in order
			var tempItemsByBucket = new Dictionary<int, List<T>>();
			foreach (var t in sample)
			{
				var bucket = valueSelector(t);
				tempItemsByBucket.GetOrCreate(bucket, _ => new List<T>()).Add(t);
			}

			var minBucket = tempItemsByBucket.Keys.Min();
			var maxBucket = tempItemsByBucket.Keys.Max();
			var currentModeCount = -1;
			var runningItemsCount = 0;
			var runningBucketSum = 0;
			MinBucketCount = int.MaxValue;
			for (int i = minBucket; i <= maxBucket; i++)
			{
				if (tempItemsByBucket.TryGetValue(i, out var items))
				{
					// Populate in order
					itemsByBucket[i] = items;
					histogram[i] = items.Count;
					buckets.Add(i);

					if (items.Count > currentModeCount)
					{
						currentModeCount = items.Count;
						Mode = i;
					}

					runningItemsCount += items.Count;
					runningBucketSum += i * items.Count;
					MaxBucketCount = Math.Max(items.Count, MaxBucketCount);
					MinBucketCount = Math.Min(items.Count, MinBucketCount);
				}
				else
				{
					// Populate Histogram densely
					histogram[i] = 0;
					MinBucketCount = 0;
				}
			}

			Mean = (double)runningBucketSum / runningItemsCount;

			// Take the bucket-size mean over the whole range
			MeanBucketCount = (double)(Histogram.Values.Sum()) / Histogram.Count;

			ItemsCount = runningItemsCount;

			var varianceBucketCount = Histogram.Values.Select(v => (v - MeanBucketCount) * (v - MeanBucketCount)).Sum() / Histogram.Count;
			SDBucketCount = Math.Sqrt(varianceBucketCount);
		}

		public static DiscreteStatisticsResult<T> Empty => new DiscreteStatisticsResult<T>(isEmpty: true);

	}

	public static class DiscreteStatisticsResult
	{
		/// <summary>
		/// Create a new statistical result.
		/// </summary>
		/// <typeparam name="T">Item type</typeparam>
		/// <param name="sample">Set of items constituting the sample</param>
		/// <param name="valueSelector">Selector to determine the value in the distribution for each item</param>
		public static DiscreteStatisticsResult<T> Create<T>(IEnumerable<T> sample, Func<T, int> valueSelector) 
			=> sample.Any() ? new DiscreteStatisticsResult<T>(sample, valueSelector) :  DiscreteStatisticsResult<T>.Empty;

	}
}
