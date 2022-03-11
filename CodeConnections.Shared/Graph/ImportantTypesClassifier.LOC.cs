#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace CodeConnections.Graph
{
	partial class ImportantTypesClassifier
	{
		private class LOCClassifier : SimpleClassifier
		{
			protected override double GetScore(Node node) => (node as TypeNode)?.LineCount ?? -1;
		}
	}
}
