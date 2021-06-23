#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Git
{
	/// <summary>
	/// Lightweight record of a file's Git status.
	/// </summary>
	public sealed record GitInfo(string FullPath, GitStatus Status);
}
