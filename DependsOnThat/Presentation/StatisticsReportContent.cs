using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Presentation
{
	[Flags]
	public enum StatisticsReportContent
	{
		None = 0,
		General = 1,
		GraphingSpecific = 2,

		All = General | GraphingSpecific
	}
}
