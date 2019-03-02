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

		[Test]
		public void PlaybackCompletedToEnd ()
		{
			var stream = GetType ().Assembly.GetManifestResourceStream ("Commons.Music.Midi.Tests.Resources.testmidi.mid");
			var vt = new AlmostVirtualMidiTimeManager ();
			var player = new MidiPlayer (MidiMusic.Read (stream), MidiAccessManager.Empty, vt);
			bool completed = false, finished = false;
			player.PlaybackCompletedToEnd += () => completed = true;
			player.Finished += () => finished = true;
			Assert.IsTrue (!completed, "1 PlaybackCompletedToEnd already fired");
			Assert.IsTrue (!finished, "2 Finished already fired");
			player.PlayAsync ();
			// FIXME: we have to "sanitize" TimeManager API and behavior.
			// In particular, the player must "wait" until the timer actually proceeds to the point of time (by "Advance_Something_By()" method, can't name appropriately now) where the player already ran.
			//vt.AdvanceBy (100);
			//vt.AdvanceBy (1);
			Assert.IsTrue (!completed, "3 PlaybackCompletedToEnd already fired");
			Assert.IsTrue (!finished, "4 Finished already fired");
			vt.AdvanceBy (100000);
			vt.AdvanceBy (1);
			player.PauseAsync ();
			player.Dispose ();
			Assert.IsTrue (completed, "5 PlaybackCompletedToEnd not fired");
			Assert.IsTrue (finished, "6 Finished not fired");
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

