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
			var vt = new AlmostVirtualMidiPlayerTimeManager ();
			var player = new MidiPlayer (MidiMusic.Read (stream), new RtMidi.RtMidiAccess (), vt);
			player.PlayAsync ();
			vt.WaitBy (10000);
			player.PauseAsync ();
			player.Dispose ();
		}

		[Test]
		public void PlayPortMidi ()
		{
			var stream = GetType ().Assembly.GetManifestResourceStream ("Commons.Music.Midi.Tests.Resources.testmidi.mid");
			var vt = new AlmostVirtualMidiPlayerTimeManager ();
			var player = new MidiPlayer (MidiMusic.Read (stream), new PortMidi.PortMidiAccess (), vt);
			player.PlayAsync ();
			vt.WaitBy (10000);
			player.PauseAsync ();
			player.Dispose ();
		}

		[Test]
		public void PlaybackCompletedToEnd ()
		{
			var stream = GetType ().Assembly
				.GetManifestResourceStream ("Commons.Music.Midi.Tests.Resources.testmidi.mid");
			using (var vt = new VirtualMidiPlayerTimeManager ()) {
				var player = new MidiPlayer (MidiMusic.Read (stream), MidiAccessManager.Empty, vt);
				bool completed = false, finished = false;
				player.PlaybackCompletedToEnd += () => completed = true;
				player.Finished += () => finished = true;
				Assert.IsTrue (!completed, "1 PlaybackCompletedToEnd already fired");
				Assert.IsTrue (!finished, "2 Finished already fired");
				player.PlayAsync ();
				vt.ProceedBy (100);
				Assert.IsTrue (!completed, "3 PlaybackCompletedToEnd already fired");
				Assert.IsTrue (!finished, "4 Finished already fired");
				vt.ProceedBy (199900);
				player.PauseAsync ();
				player.Dispose ();
				Assert.IsTrue (completed, "5 PlaybackCompletedToEnd not fired");
				Assert.IsTrue (finished, "6 Finished not fired");
			}
		}
		[Test]
		public void PlaybackCompletedToEndAbort ()
		{
			var stream = GetType ().Assembly
				.GetManifestResourceStream ("Commons.Music.Midi.Tests.Resources.testmidi.mid");
			// abort case
			using (var vt = new VirtualMidiPlayerTimeManager ()) {
				var player = new MidiPlayer (MidiMusic.Read (stream), MidiAccessManager.Empty, vt);
				bool completed = false, finished = false;
				player.PlaybackCompletedToEnd += () => completed = true;
				player.Finished += () => finished = true;
				player.PlayAsync ();
				vt.ProceedBy (100000);
				player.PauseAsync ();
				player.Dispose (); // abort in the middle
				Assert.IsFalse( completed, "1 PlaybackCompletedToEnd fired");
				Assert.IsTrue (finished, "2 Finished not fired");
			}
		}

		public class AlmostVirtualMidiPlayerTimeManager : IMidiPlayerTimeManager
		{
			public void WaitBy (int addedMilliseconds)
			{
				Thread.Sleep (50);
			}
		}
	}
}

