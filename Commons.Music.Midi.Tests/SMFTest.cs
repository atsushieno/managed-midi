using System;
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
	}
}