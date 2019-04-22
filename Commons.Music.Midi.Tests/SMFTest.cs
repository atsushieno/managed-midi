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
	}
}
