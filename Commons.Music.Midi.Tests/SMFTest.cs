using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Commons.Music.Midi.Tests
{
	[TestFixture]
	public class SMFTest
	{
		[Test]
		public void GetBpm ()
		{
			Assert.AreEqual (120, MidiMetaType.GetBpm (new byte[] {7, 0xA1, 0x20}), "120");
			Assert.AreEqual (140, Math.Round (MidiMetaType.GetBpm (new byte[] {6, 0x8A, 0xB1})), "140");
		}

		[Test]
		public void GetTimePositionInMillisecondsForTick ()
		{
			var vt = new VirtualMidiPlayerTimeManager ();
			var player = TestHelper.GetMidiPlayer (vt);
			player.PlayAsync ();
			vt.ProceedBy (100);
			player.SeekAsync (5000);
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