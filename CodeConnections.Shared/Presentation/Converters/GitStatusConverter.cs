#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CodeConnections.Git;
using static CodeConnections.Git.GitStatus;

namespace CodeConnections.Presentation.Converters
{
	public abstract class GitStatusConverter<T> : ValueConverter<GitStatus, T?>
	{
		public T? ModifiedAndNewFileData { get; set; }
		public T? ModifiedFileData { get; set; }
		public T? NewFileData { get; set; }
		protected override T? ConvertInner(GitStatus value, object parameter, CultureInfo culture)
		{
			if (value.HasFlag(New) && value.HasFlag(Modified) && ModifiedAndNewFileData is { } modifiedAndNew)
			{
				return modifiedAndNew;
				//if (!ModifiedAndNewFileData?.Equals(default(T)) ?? false)
				//{
				//	return 
				//}
			}
			if (value.HasFlag(New))
			{
				// New takes precedence over Modified. (A node may have multiple flags if merged from multiple files with different statuses.)
				return NewFileData;
			}
			if (value.HasFlag(Modified))
			{
				return ModifiedFileData;
			}

			return default;
		}

		protected override T? ConvertNull(object parameter, CultureInfo culture) => default;
	}
}
