using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	internal class EmptyMidiInput : EmptyMidiPort, IMidiInput
	{
		static EmptyMidiInput()
		{
			Instance = new EmptyMidiInput();
		}

		public static EmptyMidiInput Instance { get; private set; }

#pragma warning disable 0067
		// will never be fired.
		public event EventHandler<MidiReceivedEventArgs> MessageReceived;
#pragma warning restore 0067

		internal override IMidiPortDetails CreateDetails()
		{
			return new EmptyMidiPortDetails("dummy_in", "Dummy MIDI Input");
		}
	}
}
