using System;
using System.Collections.Generic;
using System.Linq;
using Commons.Music.Midi.RtMidi;
using NUnit.Framework;

namespace Commons.Music.Midi.Tests
{
	[TestFixture]
	public class RtMidiTest
	{
		[Test]
		[Ignore ("https://github.com/atsushieno/managed-midi/issues/1")]
		public void RawDeviceCount ()
		{
			var input = new RtMidiInputDevice ();
			int i = input.PortCount;
			var output = new RtMidiOutputDevice ();
			int o = output.PortCount;
			// mmk exposed some bug with this code.
			output.OpenPort (0, "port0");
			
			Assert.AreEqual (i, input.PortCount, "#1");
			Assert.AreEqual (o, output.PortCount, "#2");
			output.Close ();
		}
		
		[Test]
		[Ignore ("https://github.com/atsushieno/managed-midi/issues/1")]
		public void DeviceDetails ()
		{
			var a = new RtMidiAccess ();
			var dic = new Dictionary<string,IMidiPortDetails> ();
			foreach (var i in a.Inputs)
				dic.Add (i.Id, i);
			foreach (var o in a.Outputs)
				dic.Add (o.Id, o);
				
			// mmk exposed some bug with this code.
			var devId = a.Outputs.First ().Id;
			var op = a.OpenOutputAsync (devId).Result;
			IMidiPortDetails dummy;
			foreach (var o in a.Outputs)
				if (!dic.TryGetValue (o.Id, out dummy))
					Assert.Fail ("Device ID " + o.Id + " was not found.");
			op.Dispose ();
		}
	}
}

