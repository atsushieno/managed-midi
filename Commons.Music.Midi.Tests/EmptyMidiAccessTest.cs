using NUnit.Framework;
using System;
using System.Linq;

namespace Commons.Music.Midi.Tests
{
	[TestFixture]
	public class EmptyMidiAccessTest
	{
		[Test]
		public void SimpleOutput ()
		{
			var api = MidiAccessManager.Empty;
			Assert.AreEqual (1, api.Outputs.Count (), "output count");
			Assert.AreEqual (1, api.Inputs.Count (), "input count");

			var output = api.Outputs.First ();
			output.OpenAsync ();
			output.SendAsync (new byte [3] { SmfEvent.NoteOn, 0, 0 }, 0, 0);
			output.SendAsync (new byte [3] { SmfEvent.NoteOff, 0, 0 }, 0, 0);
		}
	}
}

