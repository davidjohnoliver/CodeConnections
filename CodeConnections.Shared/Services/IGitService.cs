#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeConnections.Git;

namespace CodeConnections.Services
{
	/// <summary>
	/// Exposes methods for extracting information about the active Git repository.
	/// </summary>
	internal interface IGitService
	{
		Task<ICollection<GitInfo>> GetAllModifiedAndNewFiles(CancellationToken ct);
	}
}