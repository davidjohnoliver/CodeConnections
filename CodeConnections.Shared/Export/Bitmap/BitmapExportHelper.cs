#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CodeConnections.Export.Bitmap
{
	public static class BitmapExportHelper
	{
		public static void ExportToClipboard(ElementBitmapWrapper bitmapWrapper)
		{
			var bitmap = bitmapWrapper.GetRenderTargetBitmap(192);

			Clipboard.SetImage(bitmap);
		}
	}
}
