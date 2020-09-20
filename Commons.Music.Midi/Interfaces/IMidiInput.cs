using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public interface IMidiInput : IMidiPort, IDisposable
	{
		event EventHandler<MidiReceivedEventArgs> MessageReceived;
	}
}
