using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public class MidiConnectionStateDetectorExtension
	{
		public event EventHandler<MidiConnectionEventArgs> StateChanged;
	}
}
