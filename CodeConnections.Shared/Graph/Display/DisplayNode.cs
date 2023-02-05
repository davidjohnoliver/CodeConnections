#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Git;
using CodeConnections.Presentation;

namespace CodeConnections.Graph.Display
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
		public string? ContainingProject { get; }

		private GitStatus? _gitStatus;
		public GitStatus? GitStatus { get => _gitStatus; set => OnValueSet(ref _gitStatus, value); }

		private Importance _importance;
		public Importance Importance { get => _importance; set => OnValueSet(ref _importance, value); }

		public bool IsImportant => Select(Importance, nameof(Importance), importance => importance != Importance.None);

		private int _linesOfCode;
		public int LinesOfCode { get => _linesOfCode; set => OnValueSet(ref _linesOfCode, value); }

		private int _numberOfDependents;
		public int NumberOfDependents { get => _numberOfDependents; set => OnValueSet(ref _numberOfDependents, value); }

		private int _numberOfDependencies;
		public int NumberOfDependencies { get => _numberOfDependencies; set => OnValueSet(ref _numberOfDependencies, value); }

		private double _combinedImportanceScore;
		public double CombinedImportanceScore { get => _combinedImportanceScore; set => OnValueSet(ref _combinedImportanceScore, value); }

		public DisplayNode(
			string displayString,
			NodeKey key,
			string? filePath,
			bool isPinned,
			GitStatus? gitStatus,
			string? containingProject,
			int linesOfCode,
			Importance importance,
			int numberOfDependents,
			int numberOfDependencies,
			double CombinedImportanceScore,
			object? parentContext
		)
		{
			DisplayString = displayString;
			Key = key;
			FilePath = filePath;
			IsPinned = isPinned;
			ParentContext = parentContext;
			ContainingProject = containingProject;
			GitStatus = gitStatus;
			LinesOfCode = linesOfCode;
			Importance = importance;
			_numberOfDependents = numberOfDependents;
			_numberOfDependencies = numberOfDependencies;
			_combinedImportanceScore = CombinedImportanceScore;
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
			Importance = updateTemplate.Importance;
			LinesOfCode = updateTemplate.LinesOfCode;
			NumberOfDependents = updateTemplate.NumberOfDependents;
			NumberOfDependencies = updateTemplate.NumberOfDependencies;
			CombinedImportanceScore = updateTemplate.CombinedImportanceScore;
		}
	}
}
