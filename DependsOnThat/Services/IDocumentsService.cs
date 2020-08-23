#nullable enable

namespace DependsOnThat.Services
{
	/// <summary>
	/// Exposes methods for querying current documents.
	/// </summary>
	internal interface IDocumentsService
	{
		/// <summary>
		/// Get the currently active document (ie the focused tab) if any.
		/// </summary>
		string? GetActiveDocument();
	}
}