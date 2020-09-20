using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Commons.Music.Midi
{
	public struct MidiEvent
	{
		public const byte NoteOff = 0x80;
		public const byte NoteOn = 0x90;
		public const byte PAf = 0xA0;
		public const byte CC = 0xB0;
		public const byte Program = 0xC0;
		public const byte CAf = 0xD0;
		public const byte Pitch = 0xE0;
		public const byte SysEx1 = 0xF0;
		public const byte MtcQuarterFrame = 0xF1;
		public const byte SongPositionPointer = 0xF2;
		public const byte SongSelect = 0xF3;
		public const byte TuneRequest = 0xF6;
		public const byte SysEx2 = 0xF7;
		public const byte MidiClock = 0xF8;
		public const byte MidiTick = 0xF9;
		public const byte MidiStart = 0xFA;
		public const byte MidiContinue = 0xFB;
		public const byte MidiStop = 0xFC;
		public const byte ActiveSense = 0xFE;
		public const byte Reset = 0xFF;

		public const byte EndSysEx = 0xF7;
		public const byte Meta = 0xFF;

		public static IEnumerable<MidiEvent> Convert(byte[] bytes, int index, int size)
		{
			int i = index;
			int end = index + size;
			while (i < end)
			{
				if (bytes[i] == 0xF0)
				{
					yield return new MidiEvent(0xF0, 0, 0, bytes, index, size);
					i += size;
				}
				else
				{
					var z = MidiEvent.FixedDataSize(bytes[i]);
					if (end < i + z)
						throw new Exception(string.Format(
							"Received data was incomplete to build MIDI status message for '{0:X}' status.",
							bytes[i]));
					yield return new MidiEvent(bytes[i],
						(byte)(z > 0 ? bytes[i + 1] : 0),
						(byte)(z > 1 ? bytes[i + 2] : 0),
						null, 0, 0);
					i += z + 1;
				}
			}
		}
		
		public MidiEvent(int value)
		{
			Value = value;
#pragma warning disable 618
			Data = null;
#pragma warning restore
			ExtraData = null;
			ExtraDataOffset = 0;
			ExtraDataLength = 0;
		}

		public MidiEvent(byte type, byte arg1, byte arg2, byte[] data)
			: this(type, arg1, arg2, data, 0, data != null ? data.Length : 0)
		{
		}

		public MidiEvent(byte type, byte arg1, byte arg2)
			: this(type, arg1, arg2, null, 0, 0)
		{
		}

		public MidiEvent(byte type, byte arg1, byte arg2, byte arg3)
			: this(type, arg1, arg2, new byte[] { arg3 }, 0, 1)
		{
		}

		public MidiEvent(byte type, byte arg1, byte arg2, byte[] extraData, int extraDataOffset, int extraDataLength)
		{
			Value = type + (arg1 << 8) + (arg2 << 16);
#pragma warning disable 618
			Data = extraData;
#pragma warning restore
			ExtraData = extraData;
			ExtraDataOffset = extraDataOffset;
			ExtraDataLength = extraDataLength;

		}

		public readonly int Value;

		// This expects EndSysEx byte _inclusive_ for F0 message.
		[Obsolete("Use ExtraData with ExtraDataOffset and ExtraDataLength instead.")]
		public readonly byte[] Data;

		public readonly byte[] ExtraData;

		public readonly int ExtraDataOffset;

		public readonly int ExtraDataLength;

		public byte StatusByte
		{
			get { return (byte)(Value & 0xFF); }
		}

		public byte EventType
		{
			get
			{
				switch (StatusByte)
				{
					case Meta:
					case SysEx1:
					case SysEx2:
						return StatusByte;
					default:
						return (byte)(Value & 0xF0);
				}
			}
		}

		public byte Msb
		{
			get { return (byte)((Value & 0xFF00) >> 8); }
		}

		public byte Lsb
		{
			get { return (byte)((Value & 0xFF0000) >> 16); }
		}

		public byte MetaType
		{
			get { return Msb; }
		}

		public byte Channel
		{
			get { return (byte)(Value & 0x0F); }
		}

		public static byte FixedDataSize(byte statusByte)
		{
			switch (statusByte & 0xF0)
			{
				case 0xF0: // and 0xF7, 0xFF
					switch (statusByte)
					{
						case MtcQuarterFrame:
						case SongSelect:
							return 1;
						case SongPositionPointer:
							return 2;
						default:
							return 0; // no fixed data
					}
				case Program:
				case CAf:
					return 1;
				default:
					return 2;
			}
		}

		public override string ToString()
		{
			return String.Format("{0:X02}:{1:X02}:{2:X02}{3}", StatusByte, Msb, Lsb, ExtraData != null ? "[data:" + ExtraDataLength + "]" : "");
		}
	}
}
