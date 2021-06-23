#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeConnections.Extensions;
using CodeConnections.Roslyn;
using CodeConnections.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Debugger.Evaluation.IL;

namespace CodeConnections.Graph
{
	partial class NodeGraph
	{
		/// <summary>
		/// Nodes that need to be updated.
		/// </summary>
		private readonly Queue<Node> _invalidatedNodes = new Queue<Node>();
		/// <summary>
		/// Nodes that have already been invalidated once in the current update, so we don't invalidate them again.
		/// </summary>
		private readonly HashSet<Node> _alreadyInvalidatedNodes = new HashSet<Node>();

		private bool _isUpdating = false;
		private readonly object _gate = new object();

		/// <summary>
		/// Updates node connections for modifications to the solution.
		/// </summary>
		/// <param name="compilationCache">Compilation cache for the current solution</param>
		/// <param name="invalidatedDocuments">The documents in the solution that have been invalidated</param>
		/// <param name="ct">A cancellation token. Note that the expected usage is that an update will only be cancelled if the graph is no 
		/// longer needed (eg the current open solution changes).</param>
		/// <returns>A list of nodes whose connectivity has changed.</returns>
		public async Task<ICollection<Node>> Update(CompilationCache compilationCache, IEnumerable<DocumentId> invalidatedDocuments, CancellationToken ct)
		{
			// TODO: documents with compilation errors - should probably be reevaluated on every update?

			lock (_gate)
			{
				if (_isUpdating)
				{
					throw new InvalidOperationException("Update already in process.");
				}

				_isUpdating = true;
			}

			try
			{
				foreach (var doc in invalidatedDocuments)
				{
					if (ct.IsCancellationRequested)
					{
						return ArrayUtils.GetEmpty<Node>();
					}

					await UpdateForDocument(doc, compilationCache, ct);
				}

				var allModified = new HashSet<Node>();
				while (_invalidatedNodes.Count > 0)
				{

					if (ct.IsCancellationRequested)
					{
						return allModified;
					}

					var node = _invalidatedNodes.Dequeue();
					var modifiedNodes = await UpdateNode(node, compilationCache, ct);
					allModified.UnionWith(modifiedNodes);
				}

				return allModified;
			}
			finally
			{
				_alreadyInvalidatedNodes.Clear();
				lock (_gate)
				{
					_isUpdating = false;
				}
			}
		}

		private async Task UpdateForDocument(DocumentId documentId, CompilationCache compilationCache, CancellationToken ct)
		{
			var document = compilationCache.GetDocument(documentId);
			if (document == null)
			{
				return;
			}

			var filePath = document.FilePath;
			var associatedExistingNodes = GetAssociatedNodes(filePath);

			var syntaxRoot = await document.GetSyntaxRootAsync(ct);
			if (ct.IsCancellationRequested)
			{
				return;
			}

			if (syntaxRoot == null)
			{
				return;
			}

			var model = await compilationCache.GetSemanticModel(syntaxRoot, document.Project.ToIdentifier(), ct);
			if (ct.IsCancellationRequested)
			{
				return;
			}

			if (model == null)
			{
				return;
			}

			var declaredSymbols = GetIncludedSymbolsFromSyntaxRoot(syntaxRoot, model);
			// For each symbol, get existing or new node, and invalidate it so it will be reevaluated in the next part of the Update pass.
			var declaredSymbolNodes = declaredSymbols.Select(s => GetOrCreateNode(s)).ToHashSet();
			foreach (var node in declaredSymbolNodes)
			{
				InvalidateNode(node);
			}

			// If any associatedExistingNode is no longer found in this document, invalidate it for reevaluation.
			foreach (var node in associatedExistingNodes)
			{
				if (!declaredSymbolNodes.Contains(node))
				{
					InvalidateNode(node);
				}
			}
		}

		private async Task<ICollection<Node>> UpdateNode(Node node, CompilationCache compilationCache, CancellationToken ct)
		{
			var (symbol, project) = await compilationCache.GetSymbolForNode(node, ct);

			if (symbol == null)
			{
				// It's gone! Clear node (remove all its forward links and back links). Back links need to be invalidated.
				var dirtied = new HashSet<Node>();
				var lp = new LoopProtection();
				while (node.ForwardLinks.Count > 0)
				{
					lp.Iterate();
					var forwardLink = node.ForwardLinks.First();
					dirtied.Add(forwardLink.Dependency);
					node.RemoveForwardLink(forwardLink);
				}

				while (node.BackLinks.Count > 0)
				{
					lp.Iterate();
					var backLink = node.BackLinks.First();
					var backLinkNode = backLink.Dependent;
					dirtied.Add(backLinkNode);
					backLinkNode.RemoveForwardLink(node);
					InvalidateNode(backLinkNode);
				}

				return dirtied;
			}

			// Reconcile AssociatedFiles (including _nodesByDocument)
			var associated = GetAssociatedFiles(symbol);
			var fileDiffs = associated.GetUnorderedDiff(node.AssociatedFiles);
			if (fileDiffs.IsDifferent)
			{
				foreach (var added in fileDiffs.Added)
				{
					AddAssociatedFile(node, added);
				}

				foreach (var removed in fileDiffs.Removed)
				{
					RemoveAssociatedFile(node, removed);
				}
			}

			var dependencySymbols = await symbol.GetTypeDependencies(compilationCache, project, includeExternalMetadata: false, ct)
				.Where(IsSymbolIncluded)
				.ToListAsync();

			var symbolsForDependencies = dependencySymbols.ToDictionary(s => (Node)GetOrCreateNode(s));

			var dependencies = symbolsForDependencies.Keys;
			if (ct.IsCancellationRequested)
			{
				return ArrayUtils.GetEmpty<Node>();
			}

			var diffs = dependencies.GetUnorderedDiff(node.ForwardLinkNodes);

			if (diffs.IsDifferent)
			{
				var dirtied = new HashSet<Node>();
				dirtied.Add(node);

				foreach (var removedItem in diffs.Removed)
				{
					node.RemoveForwardLink(removedItem);
					dirtied.Add(removedItem);
				}

				foreach (var addedItem in diffs.Added)
				{
					if (addedItem != node)
					{
						var linkType = GetLinkType(symbolsForDependencies[addedItem], symbol);
						node.AddForwardLink(addedItem, linkType);
					}
					dirtied.Add(addedItem);
				}

				return dirtied;
			}

			return ArrayUtils.GetEmpty<Node>();
		}

		/// <summary>
		/// Mark <paramref name="node"/> as dirty and needing updating.
		/// </summary>
		/// <remarks>This is always called during an update, and will be evaluated before the update completes (unless <paramref name="node"/> has
		/// already been invalidated during the current update).</remarks>
		private void InvalidateNode(Node node)
		{
			if (_alreadyInvalidatedNodes.Add(node))
			{
				_invalidatedNodes.Enqueue(node);
			}
		}
	}
}
