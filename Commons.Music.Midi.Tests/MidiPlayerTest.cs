using System;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

namespace Commons.Music.Midi.Tests
{
	[TestFixture]
	public class MidiPlayerTest
	{
		[Test]
		public void PlaySimple ()
		{
			var stream = GetType ().Assembly.GetManifestResourceStream ("Commons.Music.Midi.Tests.Resources.testmidi.mid");
			var vt = new VirtualMidiTimeManager ();
			var player = new MidiPlayer (MidiMusic.Read (stream), MidiAccessManager.Empty, vt);
			player.PlayAsync ();
			vt.AdvanceBy (10000);
			player.PauseAsync ();
			player.Dispose ();
		}

		[Test]
		public void PlayRtMidi ()
		{
			var stream = GetType ().Assembly.GetManifestResourceStream ("Commons.Music.Midi.Tests.Resources.testmidi.mid");
			var vt = new AlmostVirtualMidiTimeManager ();
			var player = new MidiPlayer (MidiMusic.Read (stream), new RtMidi.RtMidiAccess (), vt);
			player.PlayAsync ();
			vt.AdvanceBy (10000);
			player.PauseAsync ();
			player.Dispose ();
		}

		[Test]
		public void PlayPortMidi ()
		{
			var stream = GetType ().Assembly.GetManifestResourceStream ("Commons.Music.Midi.Tests.Resources.testmidi.mid");
			var vt = new AlmostVirtualMidiTimeManager ();
			var player = new MidiPlayer (MidiMusic.Read (stream), new PortMidi.PortMidiAccess (), vt);
			player.PlayAsync ();
			vt.AdvanceBy (10000);
			player.PauseAsync ();
			player.Dispose ();
		}

		public class AlmostVirtualMidiTimeManager : MidiTimeManagerBase
		{
			public override void AdvanceBy (int addedMilliseconds)
			{
				base.AdvanceBy (addedMilliseconds);
				Thread.Sleep (50);
			}
		}
	}
}

