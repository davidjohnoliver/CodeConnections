#nullable enable

using System;
using System.Collections.Generic;
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
		public DependencyGraphLayout()
		{
			OverlapRemovalParameters = new OverlapRemovalParameters() { HorizontalGap = 10, VerticalGap = 10 };
			OverlapRemovalAlgorithmType = "FSA";
			AsyncCompute = true;

			LayoutAlgorithmType = "LinLog"; // TODO: should be user-configurable
		}



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
			// Workaround for XamlParseException when binding directly to Graph property https://stackoverflow.com/questions/13007129/method-or-operation-not-implemented-error-on-binding
			Graph = newValue as IBidirectionalGraph<DisplayNode, DisplayEdge>;
		}
	}
}
