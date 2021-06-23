#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CodeConnections.Extensions;
using CodeConnections.Graph.Display;
using CodeConnections.Views.Graph;
using GraphSharp.Algorithms.OverlapRemoval;
using GraphSharp.AttachedBehaviours;
using GraphSharp.Controls;
using QuickGraph;
using _IDisplayGraph = QuickGraph.IBidirectionalGraph<CodeConnections.Graph.Display.DisplayNode, CodeConnections.Graph.Display.DisplayEdge>;

namespace CodeConnections.Views
{
	public class DependencyGraphLayout : GraphLayout<DisplayNode, DisplayEdge, _IDisplayGraph?>
	{
		private readonly Stopwatch _stopwatch = new Stopwatch();

		public DependencyGraphLayout()
		{
			LayoutAlgorithmFactory = new CCLayoutAlgorithmFactory<DisplayNode, DisplayEdge, _IDisplayGraph?>();
			OverlapRemovalParameters = new OverlapRemovalParameters() { HorizontalGap = 10, VerticalGap = 10 };
			OverlapRemovalAlgorithmType = "FSA";
			AsyncCompute = true;
			AnimationLength = TimeSpan.FromMilliseconds(200);

			HighlightAlgorithmType = "Simple";
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
			SelectedVertexControl = null;
			// Workaround for XamlParseException when binding directly to Graph property https://stackoverflow.com/questions/13007129/method-or-operation-not-implemented-error-on-binding
			var newGraph = newValue as _IDisplayGraph;
			IsBusy = newGraph?.VertexCount > 0;
			ApplyLightUpdate(newGraph);

			Graph = newGraph;
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs - measuring low-level timing information
			Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)CompleteTimer);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
		}

		/// <summary>
		/// Update mutable values on vertices currently in use, to preempt costly layout updates when not needed.
		/// </summary>
		/// <param name="graph">The new graph about to be applied.</param>
		private void ApplyLightUpdate(_IDisplayGraph? graph)
		{
			if (graph != null && Graph != null)
			{
				var dict = graph.Vertices.ToDictionary(v => v.Key);

				foreach (var displayNode in VertexControls.Keys)
				{
					if (dict.GetOrDefault(displayNode.Key) is { } newNode)
					{
						displayNode.UpdateNode(newNode);
					}
				}
			}
		}

		private void StartTimer()
		{
			RenderTime = null;
			_stopwatch.Reset();
			_stopwatch.Start();
		}

		private void CompleteTimer()
		{
			// TODO: this should probably be in OnLayoutFinished
			_stopwatch.Stop();
			if (VertexControls.Count > 0)
			{
				RenderTime = _stopwatch.Elapsed;
			}
		}

		// Workaround for XamlParseException when binding directly to Graph property https://stackoverflow.com/questions/13007129/method-or-operation-not-implemented-error-on-binding
		public string AlgorithmType
		{
			get { return (string)GetValue(AlgorithmTypeProperty); }
			set { SetValue(AlgorithmTypeProperty, value); }
		}

		public static readonly DependencyProperty AlgorithmTypeProperty =
			DependencyProperty.Register("AlgorithmType", typeof(string), typeof(DependencyGraphLayout), new PropertyMetadata("", (o, e) => ((DependencyGraphLayout)o).LayoutAlgorithmType = (string)e.NewValue));


		public int RandomSeed
		{
			get { return (int)GetValue(RandomSeedProperty); }
			set { SetValue(RandomSeedProperty, value); }
		}

		public static readonly DependencyProperty RandomSeedProperty =
			DependencyProperty.Register("RandomSeed", typeof(int), typeof(DependencyGraphLayout), new PropertyMetadata(0, (o, e) => ((DependencyGraphLayout)o).OnRandomSeedChanged((int)e.OldValue, (int)e.NewValue)));

		private void OnRandomSeedChanged(int oldValue, int newValue)
		{
			if (LayoutAlgorithmFactory is CCLayoutAlgorithmFactory<DisplayNode, DisplayEdge, _IDisplayGraph> factory)
			{
				factory.RandomSeed = newValue;
			}
		}

		public bool IsBusy
		{
			get { return (bool)GetValue(IsBusyProperty); }
			set { SetValue(IsBusyProperty, value); }
		}

		public static readonly DependencyProperty IsBusyProperty =
			DependencyProperty.Register("IsBusy", typeof(bool), typeof(DependencyGraphLayout), new PropertyMetadata(false));


		public VertexControl? SelectedVertexControl
		{
			get { return (VertexControl?)GetValue(SelectedVertexControlProperty); }
			set { SetValue(SelectedVertexControlProperty, value); }
		}

		public static readonly DependencyProperty SelectedVertexControlProperty =
			DependencyProperty.Register("SelectedVertexControl", typeof(VertexControl), typeof(DependencyGraphLayout), new PropertyMetadata(null,
				(o, e) => ((DependencyGraphLayout)o).OnSelectedVertexControlChanged((VertexControl)e.OldValue, (VertexControl)e.NewValue)));

		private void OnSelectedVertexControlChanged(VertexControl? oldValue, VertexControl? newValue)
		{
			if (oldValue != null)
			{
				GraphElementBehaviour.SetHighlightTrigger(oldValue, false);
			}
			if (newValue != null)
			{
				GraphElementBehaviour.SetHighlightTrigger(newValue, true);
			}
			SelectedVertex = newValue?.Vertex as DisplayNode;
		}

		public DisplayNode? SelectedVertex
		{
			get { return (DisplayNode)GetValue(SelectedVertexProperty); }
			set { SetValue(SelectedVertexProperty, value); }
		}

		public static readonly DependencyProperty SelectedVertexProperty =
			DependencyProperty.Register("SelectedVertex", typeof(DisplayNode), typeof(DependencyGraphLayout), new PropertyMetadata(null,
				(o, e) => ((DependencyGraphLayout)o).OnSelectedVertexChanged((DisplayNode)e.OldValue, (DisplayNode)e.NewValue)));

		private void OnSelectedVertexChanged(DisplayNode? oldValue, DisplayNode? newValue)
		{
			SelectedVertexControl = newValue != null ? VertexControls.GetOrDefault(newValue) : null;
		}

		protected override void CreateVertexControl(DisplayNode vertex)
		{
			base.CreateVertexControl(vertex);
			var newControl = VertexControls[vertex];

			var isDrag = DragBehaviour.GetIsDragEnabled(newControl);
			DragBehaviour.SetIsDragEnabled(newControl, false); //
			newControl.MouseLeftButtonUp += OnVertexControlMouseLeftButtonUp;
			DragBehaviour.SetIsDragEnabled(newControl, isDrag);
		}

		private void OnVertexControlMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var vertexControl = sender as VertexControl;
			if (SelectedVertexControl == vertexControl)
			{
				// Deselection
				SelectedVertexControl = null;
			}
			else
			{
				SelectedVertexControl = vertexControl;
			}
		}

		protected override void RemoveVertexControl(DisplayNode vertex)
		{
			var oldControl = VertexControls[vertex];
			base.RemoveVertexControl(vertex);
			oldControl.MouseLeftButtonUp -= OnVertexControlMouseLeftButtonUp;
		}

		protected override void OnLayoutFinished()
		{
			base.OnLayoutFinished();
			IsBusy = false;
		}
	}
}
