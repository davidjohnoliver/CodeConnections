#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DependsOnThat.Services
{
	/// <summary>
	/// Exposes methods for extracting information about the active Git repository.
	/// </summary>
	internal interface IGitService
	{
		Task<ICollection<string>> GetAllModifiedAndNewFiles(CancellationToken ct);
	}
}