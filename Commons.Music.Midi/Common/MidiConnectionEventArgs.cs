using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public class MidiConnectionEventArgs : EventArgs
	{
		public IMidiPortDetails Port { get; private set; }
	}

}
