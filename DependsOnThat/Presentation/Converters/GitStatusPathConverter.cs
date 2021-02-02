#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using DependsOnThat.Git;
using static DependsOnThat.Git.GitStatus;

namespace DependsOnThat.Presentation.Converters
{
	public class GitStatusPathConverter : ValueConverter<GitStatus, Geometry?>
	{
		public Geometry? ModifiedFileData { get; set; }
		public Geometry? NewFileData { get; set; }

		protected override Geometry? ConvertInner(GitStatus value, object parameter, CultureInfo culture) => value switch
		{
			Modified => ModifiedFileData,
			New => NewFileData,
			_ => null
		};

		protected override Geometry? ConvertNull(object parameter, CultureInfo culture) => null;
	}
}
