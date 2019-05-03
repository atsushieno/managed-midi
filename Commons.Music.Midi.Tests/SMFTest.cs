using System;
using System.Linq;
using System.Linq.Expressions;
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
			Assert.AreEqual (120, MidiMetaType.GetBpm (new byte[] {7, 0xA1, 0x20}, 0), "120");
			Assert.AreEqual (140, Math.Round (MidiMetaType.GetBpm (new byte[] {6, 0x8A, 0xB1}, 0)), "140");
		}

		[Test]
		public void GetTempo ()
		{
			Assert.AreEqual (500000, MidiMetaType.GetTempo (new byte[] {7, 0xA1, 0x20}, 0), "500000");
		}
		
		[Test]
		public void GetFixedSize ()
		{
			Assert.AreEqual (2, MidiEvent.FixedDataSize (0x90), "NoteOn");
			Assert.AreEqual (1, MidiEvent.FixedDataSize (0xC0), "ProgramChange");
			Assert.AreEqual (1, MidiEvent.FixedDataSize (0xD0), "CAf");
			Assert.AreEqual (2, MidiEvent.FixedDataSize (0xA0), "PAf");
			Assert.AreEqual (0, MidiEvent.FixedDataSize (0xF0), "SysEx");
			Assert.AreEqual (2, MidiEvent.FixedDataSize (0xF2), "SongPositionPointer");
			Assert.AreEqual (1, MidiEvent.FixedDataSize (0xF3), "SongSelect");
			Assert.AreEqual (0, MidiEvent.FixedDataSize (0xF8), "MidiClock");
			Assert.AreEqual (0, MidiEvent.FixedDataSize (0xFF), "META");
		}

		[Test]
		public void MidiEventConvert ()
		{
			var bytes1 = new byte [] {0xF8};
			var events1 = MidiEvent.Convert (bytes1, 0, bytes1.Length);
			Assert.AreEqual (1, events1.Count (), "bytes1 count");

			var bytes2 = new byte [] {0xFE};
			var events2 = MidiEvent.Convert (bytes2, 0, bytes2.Length);
			Assert.AreEqual (1, events2.Count (), "bytes2 count");
		}

		[Test]
		public void MidiMusicGetPlayTimeMillisecondsAtTick ()
		{
			var music = TestHelper.GetMidiMusic ();
			Assert.AreEqual (0, music.GetTimePositionInMillisecondsForTick (0), "tick 0");
			Assert.AreEqual (125, music.GetTimePositionInMillisecondsForTick (48), "tick 48");
			Assert.AreEqual (500, music.GetTimePositionInMillisecondsForTick (192), "tick 192");
		}

		// FIXME: this test seems to be order/position dependent.
		// It should be moved to MidiPlayerTest class, but it caused regression.
		// 
		// It is likely related to manifest resource tetrieval.
		[Test]
		public void GetTimePositionInMillisecondsForTick ()
		{
			var vt = new VirtualMidiPlayerTimeManager ();
			var player = TestHelper.GetMidiPlayer (vt);
			player.Play ();
			vt.ProceedBy (100);
			player.Seek (5000);
			Task.Delay (200);
			Assert.AreEqual (5000, player.PlayDeltaTime, "1 PlayDeltaTime");
			Assert.AreEqual (12, (int) player.PositionInTime.TotalSeconds, "1 PositionInTime");
			vt.ProceedBy (100);
			// FIXME: this is ugly.
			Task.Delay (100);
			// FIXME: not working
			//Assert.AreEqual (5100, player.PlayDeltaTime, "2 PlayDeltaTime");
			Assert.AreEqual (12, (int) player.PositionInTime.TotalSeconds, "2 PositionInTime");
			player.Seek (2000);
			Assert.AreEqual (2000, player.PlayDeltaTime, "3 PlayDeltaTime");
			Assert.AreEqual (5, (int) player.PositionInTime.TotalSeconds, "3 PositionInTime");
			vt.ProceedBy (100);
			// FIXME: this is ugly.
			Task.Delay (100);
			// FIXME: not working
			//Assert.AreEqual (2100, player.PlayDeltaTime, "4 PlayDeltaTime");
			Assert.AreEqual (5, (int) player.PositionInTime.TotalSeconds, "4 PositionInTime");
		}

		[Test]
		public void SmfReaderRead ()
		{
			foreach (var name in GetType ().Assembly.GetManifestResourceNames ()) {
				using (var stream = GetType ().Assembly.GetManifestResourceStream (name)) {
					try {
						new SmfReader ().Read (stream);
					}
					catch {
						Assert.Warn ($"Failed at {name}");
						throw;
					}
				}
			}
		}
	}
}
