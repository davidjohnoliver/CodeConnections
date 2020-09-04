#nullable enable

using System;

namespace DependsOnThat.Services
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
		/// Raised when the active open document changes
		/// </summary>
		event Action? ActiveDocumentChanged;
	}
}