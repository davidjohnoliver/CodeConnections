#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependsOnThat.Git;

namespace DependsOnThat.Presentation.Converters
{
	public class GitStatusTooltipConverter : ValueConverter<GitStatus, string?>
	{
		protected override string? ConvertInner(GitStatus value, object parameter, CultureInfo culture) => value switch
		{
			GitStatus.Modified => "Modified File",
			GitStatus.New => "New File",
			_ => null
		};

		protected override string? ConvertNull(object parameter, CultureInfo culture) => null;
	}
}
