#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DependsOnThat.Git;
using DependsOnThat.Graph.Display;

namespace DependsOnThat.Presentation.Converters
{
	public class DisplayNodeGitIndicatorVisibilityConverter : MultiValueConverter<GitStatus, bool, Visibility>
	{
		protected override Visibility ConvertInner(GitStatus value1, bool value2, object parameter, CultureInfo culture)
		{
			if (value1 != GitStatus.Unchanged && value2)
			{
				return Visibility.Visible;
			}

			return Visibility.Collapsed;
		}

		protected override Visibility ConvertUnexpected(object parameter, CultureInfo culture) => Visibility.Collapsed;
	}
}
