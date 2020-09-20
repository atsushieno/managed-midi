using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Commons.Music.Midi
{
	public class MidiMessage
	{
		public MidiMessage(int deltaTime, MidiEvent evt)
		{
			DeltaTime = deltaTime;
			Event = evt;
		}
		public byte[] RawData => GetRawData();

		public readonly int DeltaTime;
		public readonly MidiEvent Event;

		public override string ToString()
		{
			return String.Format("[dt{0}]{1}", DeltaTime, Event);
		}

		private byte[] GetRawData()
		{
			List<byte> bytes = new List<byte>();
			switch (Event.EventType)
			{
				case MidiEvent.SysEx1:
				case MidiEvent.SysEx2:
					bytes.Add(Event.StatusByte);
					bytes.AddRange(Event.ExtraData.Skip(Event.ExtraDataOffset).Take(Event.ExtraDataLength));
					break;
				case MidiEvent.Meta:
					// do nothing.
					break;
				case MidiEvent.NoteOn:
				case MidiEvent.NoteOff:
				default:
					var size = MidiEvent.FixedDataSize(Event.StatusByte);
					bytes.Add(Event.StatusByte);
					if (size >= 1)
					{
						bytes.Add(Event.Msb);
					}
					if (size >= 2)
					{
						bytes.Add(Event.Lsb);
					}
					break;
			}
			return bytes.ToArray();
		}
	}

}
