#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeConnections.Graph
{
	partial class ImportantTypesClassifier
	{
		private class DependentsClassifier : SimpleClassifier
		{
			protected override double GetScore(Node node)
				=> node.BackLinkNodes.Count(n => n is TypeNode);
		}
	}
}
