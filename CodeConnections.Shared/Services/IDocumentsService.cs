#nullable enable

using System;

namespace CodeConnections.Services
{
	/// <summary>
	/// Exposes methods for querying and interacting with current documents.
	/// </summary>
	internal interface IDocumentsService
	{
		/// <summary>
		/// Get the currently active document (ie the focused tab) if any.
		/// </summary>
		string? GetActiveDocument();

		/// <summary>
		/// Open a document view for a file, using the preview tab if it's not already opened.
		/// </summary>
		void OpenFileAsPreview(string fileName);

		/// <summary>
		/// Whether the current active document shares a tab group with the Code Connections tool window.
		/// </summary>
		public bool IsActiveDocumentInToolTabGroup { get; }

		/// <summary>
		/// Raised when the active open document changes
		/// </summary>
		event EventHandler<ActiveDocumentChangedEventArgs>? ActiveDocumentChanged;
	}
}