#nullable enable

using Microsoft.Win32;
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
		private const int Dpi = 192;

		public static void ExportToClipboard(ElementBitmapWrapper bitmapWrapper)
		{
			var bitmap = bitmapWrapper.GetRenderTargetBitmap(Dpi);

			Clipboard.SetImage(bitmap);
		}

		public static void ExportToFile(ElementBitmapWrapper bitmapWrapper, string solutionName)
		{
			var bitmap = bitmapWrapper.GetRenderTargetBitmap(Dpi);

			var encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(bitmap));

			var dialog = new SaveFileDialog();
			dialog.FileName = $"CodeConnections_{solutionName}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
			dialog.DefaultExt = ".png";
			dialog.Filter = "PNG files|*.png";

			var ok = dialog.ShowDialog();
			if (ok ?? false)
			{
				using (var fileStream = dialog.OpenFile())
				{
					encoder.Save(fileStream);
				}
			}
		}
	}
}
