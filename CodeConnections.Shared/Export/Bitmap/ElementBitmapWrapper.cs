#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Threading.Tasks;

namespace CodeConnections.Export.Bitmap
{
	/// <summary>
	/// Wraps a visual element, allowing a <see cref="RenderTargetBitmap"/> based on it to be accessed from business logic.
	/// </summary>
	public class ElementBitmapWrapper
	{
		private readonly UIElement? _element;

		private ElementBitmapWrapper(UIElement? element)
		{
			_element = element;
		}

		const double BaseDpi = 96;

		public RenderTargetBitmap GetRenderTargetBitmap(double dpi)
		{
			return GetBitmapFromElement(_element ?? throw new InvalidOperationException(), dpi);
		}

		private static RenderTargetBitmap GetBitmapFromElement(UIElement element, double dpi)
		{
			var dimensions = GetRenderDimensions(element);

			var drawingVisual = new DrawingVisual();
			using (var context = drawingVisual.RenderOpen())
			{
				context.DrawRectangle(new VisualBrush(element), null, new Rect(0, 0, dimensions.Width, dimensions.Height));
			}
			var bitmap = new RenderTargetBitmap((int)(dimensions.Width * dpi / BaseDpi), (int)(dimensions.Height * dpi / BaseDpi), dpi, dpi, PixelFormats.Pbgra32);
			bitmap.Render(drawingVisual);

			return bitmap;
		}

		private static (double Width, double Height) GetRenderDimensions(UIElement element)
		{
			var bounds = VisualTreeHelper.GetDescendantBounds(element);
			return (bounds.Width, bounds.Height);
		}


		public static ElementBitmapWrapper GetWrapper(UIElement obj)
		{
			return (ElementBitmapWrapper)obj.GetValue(WrapperProperty);
		}

		public static void SetWrapper(UIElement obj, ElementBitmapWrapper value)
		{
			obj.SetValue(WrapperProperty, value);
		}

		// Using a DependencyProperty as the backing store for Wrapper.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty WrapperProperty =
			DependencyProperty.RegisterAttached("Wrapper", typeof(ElementBitmapWrapper), typeof(ElementBitmapWrapper), new PropertyMetadata(new ElementBitmapWrapper(null), OnWrapperChanged));

		private static async void OnWrapperChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			try
			{
				// Null will be pushed from the two-way binding when it is applied, which we use as a trigger to initialize the wrapper.
				if (e.NewValue == null && d is UIElement element)
				{
					await Task.Yield();
					SetWrapper(element, new(element));
				}
			}
			catch (Exception)
			{
				// Failed to set wrapper
			}
		}
	}
}
