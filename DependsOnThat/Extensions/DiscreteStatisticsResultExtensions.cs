using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Statistics;
using static System.Math;

namespace CodeConnections.Extensions
{
	public static class DiscreteStatisticsResultExtensions
	{
		/// <summary>
		/// Gets the items with the highest values from <paramref name="statisticsResult"/>.
		/// 
		/// This will return at least <paramref name="atLeast"/> items whose value exceeds <paramref name="minSampleValue"/> (if available). 
		/// It will try to return all 'equal best' items (ie items in the same bucket) but only up to <paramref name="noMoreThan"/>.
		/// </summary>
		public static IEnumerable<T> GetTopItems<T>(this DiscreteStatisticsResult<T> statisticsResult, int atLeast, int noMoreThan, int minSampleValue = int.MinValue)
		{
			atLeast = Min(atLeast, statisticsResult.ItemsCount);
			noMoreThan = Min(noMoreThan, statisticsResult.ItemsCount);

			var itemsReturned = 0;

			for (int i = statisticsResult.BucketValues.Count - 1; i >= 0; i--)
			{
				if (itemsReturned >= atLeast)
				{
					break;
				}
				var bucketValue = statisticsResult.BucketValues[i];
				if (bucketValue < minSampleValue)
				{
					break;
				}

				var bucket = statisticsResult.ItemsByBucket[bucketValue];
				foreach (var item in bucket)
				{
					if (itemsReturned >= noMoreThan)
					{
						break;
					}

					yield return item;
					itemsReturned++;
				}
			}
		}
	}
}
