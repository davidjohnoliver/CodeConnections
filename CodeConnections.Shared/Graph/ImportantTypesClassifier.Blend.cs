#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace CodeConnections.Graph
{
	partial class ImportantTypesClassifier
	{
		private class HouseBlendClassifier : ImportantTypesClassifier
		{
			public override IEnumerable<NodeKey> GetImportantTypes(NodeGraph fullGraph, int noRequested)
			{
				throw new NotImplementedException("TODO now");
			}
		}
	}
}
