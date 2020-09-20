using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public class MidiReceivedEventArgs : EventArgs
	{
		public long Timestamp { get; set; }
		public byte[] Data { get; set; }
		public int Start { get; set; }
		public int Length { get; set; }
	}
}
