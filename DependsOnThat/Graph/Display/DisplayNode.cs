#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Git;
using DependsOnThat.Presentation;

namespace DependsOnThat.Graph.Display
{
	/// <summary>
	/// A node in the displayable subgraph.
	/// </summary>
	public class DisplayNode : ViewModelBase
	{
		public string DisplayString { get; }
		public NodeKey Key { get; }
		public string? FilePath { get; }

		private bool _isPinned;
		public bool IsPinned { get => _isPinned; set => OnValueSet(ref _isPinned, value); }
		public object? ParentContext { get; }

		private GitStatus? _gitStatus;
		public GitStatus? GitStatus { get => _gitStatus; set => OnValueSet(ref _gitStatus, value); }

		public DisplayNode(string displayString, NodeKey key, string? filePath, bool isPinned, GitStatus? gitStatus, object? parentContext)
		{
			DisplayString = displayString;
			Key = key;
			FilePath = filePath;
			IsPinned = isPinned;
			ParentContext = parentContext;
			GitStatus = gitStatus;
		}

		public override bool Equals(object obj) => obj is DisplayNode otherNode && otherNode.DisplayString == DisplayString && otherNode.Key == Key && otherNode.GitStatus == GitStatus && otherNode.IsPinned == IsPinned;

		public override int GetHashCode() => DisplayString?.GetHashCode() ?? 0;

		public override string ToString() => $"{nameof(DisplayNode)}-{DisplayString}";

		/// <summary>
		/// Update mutable values on this node to reflect those on <paramref name="updateTemplate"/> (which is expected to be a different 
		/// version with the same <see cref="NodeKey"/>).
		/// </summary>
		public void UpdateNode(DisplayNode updateTemplate)
		{
			Debug.Assert(Equals(updateTemplate.Key, Key));

			IsPinned = updateTemplate.IsPinned;
			GitStatus = updateTemplate.GitStatus;
		}
	}
}
