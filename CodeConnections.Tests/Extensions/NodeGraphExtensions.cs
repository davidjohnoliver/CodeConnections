using CodeConnections.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Tests.Extensions
{
	public static class NodeGraphExtensions
	{
		public static Node GetNodeForType(this NodeGraph graph, string typeName) 
			=> graph.Nodes.Single(kvp => kvp.Key is TypeNodeKey tnk && tnk.Identifier.Name == typeName).Value;
	}
}
