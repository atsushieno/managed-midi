using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Commons.Music.Midi
{
	internal class EmptyMidiOutput : EmptyMidiPort, IMidiOutput
	{
		Task completed_task = Task.FromResult(false);

		static EmptyMidiOutput()
		{
			Instance = new EmptyMidiOutput();
		}

		public static EmptyMidiOutput Instance { get; private set; }

		public void Send(byte[] mevent, int offset, int length, long timestamp)
		{
			// do nothing.
		}

		internal override IMidiPortDetails CreateDetails()
		{
			return new EmptyMidiPortDetails("dummy_out", "Dummy MIDI Output");
		}
	}
}
