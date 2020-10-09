using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Services;

namespace DependsOnThat.Extensions
{
	public static class OutputServiceExtensions
	{
		/// <summary>
		/// Write multiple lines to <paramref name="outputService"/>.
		/// </summary>
		internal static void WriteLines(this IOutputService outputService, IEnumerable<string> output)
		{
			foreach (var str in output)
			{
				outputService.WriteLine(str);
			}
		}
	}
}
