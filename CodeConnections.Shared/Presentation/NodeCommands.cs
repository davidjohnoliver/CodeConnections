#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using CodeConnections.Graph;
using CodeConnections.Graph.Display;

namespace CodeConnections.Presentation
{
	/// <summary>
	/// Collection of commands that act upon a single display node.
	/// </summary>
	public sealed class NodeCommands
	{
		private readonly GraphUpdateManager _graphUpdateManager;

		private readonly Dictionary<string, ICommand> _commands = new();

		public NodeCommands(GraphUpdateManager graphUpdateManager)
		{
			this._graphUpdateManager = graphUpdateManager;
		}

		public ICommand this[string name] => _commands[name];

		public void AddOperationCommand(Func<NodeKey,Subgraph.Operation> operation, string name)
		{
			var command = SimpleCommand.Create<DisplayNode>(ExecuteOperation);
			_commands.Add(name, command);

			void ExecuteOperation(DisplayNode? displayNode)
			{
				if (displayNode != null)
				{
					var op = operation(displayNode.Key);
					_graphUpdateManager.ModifySubgraph(op);
				}
			}
		}
	}
}
