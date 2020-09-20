using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media.Midi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Commons.Music.Midi;

namespace Commons.Music.Midi.Droid
{
	public class MidiOutput : MidiPort, IMidiOutput
	{
		MidiInputPort port;

		public MidiOutput(MidiPortDetails details, MidiInputPort port)
			: base(details, () => port.Close())
		{
			this.port = port;
		}

		public void Send(byte[] mevent, int offset, int length, long timestamp)
		{
			port.Send(mevent, offset, length, timestamp);
		}
	}
}