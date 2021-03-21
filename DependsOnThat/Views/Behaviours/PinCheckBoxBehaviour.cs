using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CodeConnections.Views.Behaviours
{
	public static class PinCheckBoxBehaviour
	{
		public static bool GetForceShow(DependencyObject obj)
		{
			return (bool)obj.GetValue(ForceShowProperty);
		}

		public static void SetForceShow(DependencyObject obj, bool value)
		{
			obj.SetValue(ForceShowProperty, value);
		}

		public static readonly DependencyProperty ForceShowProperty =
			DependencyProperty.RegisterAttached("ForceShow", typeof(bool), typeof(PinCheckBoxBehaviour), new FrameworkPropertyMetadata(false, flags: FrameworkPropertyMetadataOptions.Inherits));


	}
}
