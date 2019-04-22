using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Commons.Music.Midi.Tests
{
	[TestFixture]
	public class MidiPlayerTest
	{
		public class AlmostVirtualMidiPlayerTimeManager : VirtualMidiPlayerTimeManager
		{
			public override void WaitBy (int addedMilliseconds)
			{
				Thread.Sleep (50);
				base.WaitBy (addedMilliseconds);
			}
		}
		
		[Test]
		public void PlaySimple ()
		{
			var vt = new VirtualMidiPlayerTimeManager ();
			var player = TestHelper.GetMidiPlayer (vt);
			player.PlayAsync ();
			vt.ProceedBy (200000);
			player.PauseAsync ();
			player.Dispose ();
		}

		[Ignore ("rtmidi may not be runnable depending on the test runner platform")]
		[Test]
		public void PlayRtMidi ()
		{
			var vt = new AlmostVirtualMidiPlayerTimeManager ();
			var player = TestHelper.GetMidiPlayer (vt, new RtMidi.RtMidiAccess ());
			player.PlayAsync ();
			vt.ProceedBy (200000);
			player.PauseAsync ();
			player.Dispose ();
		}

		[Ignore ("portmidi may not be runnable depending on the test runner platform")]
		[Test]
		public void PlayPortMidi ()
		{
			var vt = new AlmostVirtualMidiPlayerTimeManager ();
			var player = TestHelper.GetMidiPlayer (vt, new PortMidi.PortMidiAccess ());
			player.PlayAsync ();
			vt.ProceedBy (200000);
			player.PauseAsync ();
			player.Dispose ();
		}

		[Test]
		public void PlaybackCompletedToEnd ()
		{
			var vt = new VirtualMidiPlayerTimeManager ();
			var music = TestHelper.GetMidiMusic ();
			var qmsec = MidiMusic.GetPlayTimeMillisecondsAtTick (music.Tracks [0].Messages, 4998, 192);
			var player = TestHelper.GetMidiPlayer (vt, music);
			bool completed = false, finished = false;
			
			player.PlaybackCompletedToEnd += () => completed = true;
			player.Finished += () => finished = true;
			Assert.IsTrue (!completed, "1 PlaybackCompletedToEnd already fired");
			Assert.IsTrue (!finished, "2 Finished already fired");
			player.PlayAsync ();
			vt.ProceedBy (100);
			Assert.IsTrue (!completed, "3 PlaybackCompletedToEnd already fired");
			Assert.IsTrue (!finished, "4 Finished already fired");
			vt.ProceedBy (qmsec);
			Assert.AreEqual (12889, qmsec, "qmsec");
			// FIXME: this is ugly
			while (player.PlayDeltaTime < 4988)
				Task.Delay (100);
			Assert.AreEqual (4988, player.PlayDeltaTime, "PlayDeltaTime");
			player.PauseAsync ();
			player.Dispose ();
			Assert.IsTrue (completed, "5 PlaybackCompletedToEnd not fired");
			Assert.IsTrue (finished, "6 Finished not fired");
		}
		
		[Test]
		public void PlaybackCompletedToEndAbort ()
		{
			var vt = new VirtualMidiPlayerTimeManager ();
			var player = TestHelper.GetMidiPlayer (vt);
			bool completed = false, finished = false;
			player.PlaybackCompletedToEnd += () => completed = true;
			player.Finished += () => finished = true;
			player.PlayAsync ();
			vt.ProceedBy (1000);
			// FIXME: this is ugly
			while (player.PlayDeltaTime == 0)
				Task.Delay (100);
			player.PauseAsync ();
			player.Dispose (); // abort in the middle
			Assert.IsFalse( completed, "1 PlaybackCompletedToEnd unexpectedly fired");
			Assert.IsTrue (finished, "2 Finished not fired");
		}

		[Test]
		public void GetTimePositionInMillisecondsForTick ()
		{
			var vt = new VirtualMidiPlayerTimeManager ();
			var player = TestHelper.GetMidiPlayer (vt);
			player.PlayAsync ();
			vt.ProceedBy (100);
			player.SeekAsync (5000);
			Task.Delay (100);
			Assert.AreEqual (5000, player.PlayDeltaTime, "1 PlayDeltaTime");
			Assert.AreEqual (12, (int) player.PositionInTime.TotalSeconds, "1 PositionInTime");
			vt.ProceedBy (100);
			// FIXME: this is ugly.
			Task.Delay (100);
			// FIXME: not working
			//Assert.AreEqual (5100, player.PlayDeltaTime, "2 PlayDeltaTime");
			Assert.AreEqual (12, (int) player.PositionInTime.TotalSeconds, "2 PositionInTime");
			player.SeekAsync (2000);
			Assert.AreEqual (2000, player.PlayDeltaTime, "3 PlayDeltaTime");
			Assert.AreEqual (5, (int) player.PositionInTime.TotalSeconds, "3 PositionInTime");
			vt.ProceedBy (100);
			// FIXME: this is ugly.
			Task.Delay (100);
			// FIXME: not working
			//Assert.AreEqual (2100, player.PlayDeltaTime, "4 PlayDeltaTime");
			Assert.AreEqual (5, (int) player.PositionInTime.TotalSeconds, "4 PositionInTime");
		}
	}
}
