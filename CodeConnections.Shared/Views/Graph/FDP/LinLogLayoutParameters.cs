// https://github.com/NinetailLabs/GraphSharp/tree/4831873c0465c0738adc94c7180a417352efeb58/Graph%23/Algorithms/Layout/Simple

using GraphSharp.Algorithms.Layout;

namespace CodeConnections.Views.Graph.FDP
{
	public class LinLogLayoutParameters : LayoutParametersBase
	{
		internal double attractionExponent = 2.5;

		public double AttractionExponent
		{
			get { return attractionExponent; }
			set
			{
				attractionExponent = value;
				NotifyPropertyChanged("AttractionExponent");
			}
		}

		internal double repulsiveExponent;

		public double RepulsiveExponent
		{
			get { return repulsiveExponent; }
			set
			{
				repulsiveExponent = value;
				NotifyPropertyChanged("RepulsiveExponent");
			}
		}

		internal double gravitationMultiplier = 0.2;

		public double GravitationMultiplier
		{
			get { return gravitationMultiplier; }
			set
			{
				gravitationMultiplier = value;
				NotifyPropertyChanged("GravitationMultiplier");
			}
		}

		internal int iterationCount = 100;

		public int IterationCount
		{
			get { return iterationCount; }
			set
			{
				iterationCount = value;
				NotifyPropertyChanged("IterationCount");
			}
		}
	}
}