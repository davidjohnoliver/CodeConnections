#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DependsOnThat.Presentation;
using GraphSharp.Algorithms.OverlapRemoval;
using GraphSharp.Controls;
using QuickGraph;

namespace DependsOnThat.Views
{
	public class DependencyGraphLayout : GraphLayout<DisplayNode, DisplayEdge, IBidirectionalGraph<DisplayNode, DisplayEdge>?>
	{
		private readonly Stopwatch _stopwatch = new Stopwatch();

		public DependencyGraphLayout()
		{
			OverlapRemovalParameters = new OverlapRemovalParameters() { HorizontalGap = 10, VerticalGap = 10 };
			OverlapRemovalAlgorithmType = "FSA";
			AsyncCompute = true;

			LayoutAlgorithmType = "LinLog";
		}

		public TimeSpan? RenderTime
		{
			get { return (TimeSpan?)GetValue(RenderTimeProperty); }
			set { SetValue(RenderTimeProperty, value); }
		}

		public static readonly DependencyProperty RenderTimeProperty =
			DependencyProperty.Register("RenderTime", typeof(TimeSpan?), typeof(DependencyGraphLayout), new PropertyMetadata(null));

		public object DisplayGraph
		{
			get { return (object)GetValue(DisplayGraphProperty); }
			set { SetValue(DisplayGraphProperty, value); }
		}

		public static readonly DependencyProperty DisplayGraphProperty =
			DependencyProperty.Register("DisplayGraph", typeof(object), typeof(DependencyGraphLayout), new PropertyMetadata(
				null,
				(o, e) => ((DependencyGraphLayout)o).OnDisplayGraphChanged(e.OldValue, e.NewValue)
			));

		private void OnDisplayGraphChanged(object oldValue, object newValue)
		{
			StartTimer();
			// Workaround for XamlParseException when binding directly to Graph property https://stackoverflow.com/questions/13007129/method-or-operation-not-implemented-error-on-binding
			Graph = newValue as IBidirectionalGraph<DisplayNode, DisplayEdge>;
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs - measuring low-level timing information
			Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)CompleteTimer);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
		}

		private void StartTimer()
		{
			RenderTime = null;
			_stopwatch.Reset();
			_stopwatch.Start();
		}

		private void CompleteTimer()
		{
			_stopwatch.Stop();
			if (VertexControls.Count > 0)
			{
				RenderTime = _stopwatch.Elapsed;
			}
		}
	}
}
