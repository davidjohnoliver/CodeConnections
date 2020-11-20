﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DependsOnThat.Extensions;
using DependsOnThat.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Debugger.Evaluation.IL;

namespace DependsOnThat.Graph
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
		/// <summary>
		/// Documents that need to be updated.
		/// </summary>
		private readonly HashSet<DocumentId> _invalidatedDocuments = new HashSet<DocumentId>();

		private bool _isUpdating = false;
		private readonly object _gate = new object();

		/// <summary>
		/// Updates node connections for modifications to the solution, as determined by calls to <see cref="InvalidateDocument(DocumentId)"/> since 
		/// the beginning of the previous update.
		/// </summary>
		/// <param name="solution">The current solution</param>
		/// <param name="ct">A cancellation token. Note that the expected usage is that an update will only be cancelled if the graph is no 
		/// longer needed (eg the current open solution changes).</param>
		/// <returns>A list of nodes whose connectivity has changed.</returns>
		public async Task<ICollection<Node>> Update(Solution solution, CancellationToken ct)
		{
			// TODO: documents with compilation errors - should probably be reevaluated on every update?

			List<DocumentId> invalidatedDocuments;
			lock (_gate)
			{
				if (_isUpdating)
				{
					throw new InvalidOperationException("Update already in process.");
				}

				_isUpdating = true;
				invalidatedDocuments = _invalidatedDocuments.ToList();
				_invalidatedDocuments.Clear();
			}

			try
			{
				_compilationCache.Activate(solution);

				foreach (var doc in invalidatedDocuments)
				{
					if (ct.IsCancellationRequested)
					{
						return ArrayUtils.GetEmpty<Node>();
					}

					await UpdateForDocument(doc, solution, ct);
				}

				var allModified = new HashSet<Node>();
				while (_invalidatedNodes.Count > 0)
				{

					if (ct.IsCancellationRequested)
					{
						return allModified;
					}

					var node = _invalidatedNodes.Dequeue();
					var modifiedNodes = await UpdateNode(node, solution, ct);
					allModified.UnionWith(modifiedNodes);
				}

				return allModified;
			}
			finally
			{
				_alreadyInvalidatedNodes.Clear();
				_compilationCache.Reset();
				lock (_gate)
				{
					_isUpdating = false;
				}
			}
		}

		private async Task UpdateForDocument(DocumentId documentId, Solution solution, CancellationToken ct)
		{
			var document = solution.GetDocument(documentId);
			if (document == null)
			{
				return;
			}

			var filePath = document.FilePath;
			var associatedExistingNodes = (_nodesByDocument!).GetOrDefault(filePath);

			var syntaxRoot = await document.GetSyntaxRootAsync(ct);
			if (ct.IsCancellationRequested)
			{
				return;
			}

			if (syntaxRoot == null)
			{
				return;
			}

			var model = await _compilationCache.GetSemanticModel(syntaxRoot, document.Project.ToIdentifier(), ct);
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
			foreach (var node in associatedExistingNodes ?? Enumerable.Empty<Node>())
			{
				if (!declaredSymbolNodes.Contains(node))
				{
					InvalidateNode(node);
				}
			}
		}

		private async Task<ICollection<Node>> UpdateNode(Node node, Solution solution, CancellationToken ct)
		{
			var (symbol, compilation) = await _compilationCache.GetSymbolForNode(node, ct);

			if (symbol == null)
			{
				// It's gone! Clear node (remove all its forward links and back links). Back links need to be invalidated.
				var dirtied = new HashSet<Node>();
				var lp = new LoopProtection();
				while (node.ForwardLinks.Count > 0)
				{
					lp.Iterate();
					var forwardLink = node.ForwardLinks.First();
					dirtied.Add(forwardLink);
					node.RemoveForwardLink(forwardLink);
				}

				while (node.BackLinks.Count > 0)
				{
					lp.Iterate();
					var backLink = node.BackLinks.First();
					dirtied.Add(backLink);
					backLink.RemoveForwardLink(node);
					InvalidateNode(backLink);
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

			var dependencies = await symbol.GetTypeDependencies(compilation!, includeExternalMetadata: false, ct).Select(s => GetOrCreateNode(s)).ToListAsync();
			if (ct.IsCancellationRequested)
			{
				return ArrayUtils.GetEmpty<Node>();
			}

			var diffs = dependencies.GetUnorderedDiff(node.ForwardLinks);

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
						node.AddForwardLink(addedItem);
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

		/// <summary>
		/// Mark <paramref name="documentId"/> as dirty. Nodes associated with it will be updated the next time the graph updates.
		/// </summary>
		/// <remarks>If called during an active update, it will be applied during the subsequent update.</remarks>
		public void InvalidateDocument(DocumentId documentId)
		{
			lock (_gate)
			{
				_invalidatedDocuments.Add(documentId);
			}
		}
	}
}
