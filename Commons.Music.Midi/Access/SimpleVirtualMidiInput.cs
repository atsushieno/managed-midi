using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public class SimpleVirtualMidiInput : SimpleVirtualMidiPort, IMidiInput
	{
		public SimpleVirtualMidiInput(IMidiPortDetails details, Action onDispose)
			: base(details, onDispose)
		{
		}

		public event EventHandler<MidiReceivedEventArgs> MessageReceived;
	}
}
