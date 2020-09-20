using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Commons.Music.Midi
{
	public class SmfWriter
	{
		Stream stream;

		public SmfWriter(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");
			this.stream = stream;

			// default meta event writer.
			meta_event_writer = SmfWriterExtension.DefaultMetaEventWriter;
		}

		public bool DisableRunningStatus { get; set; }

		void WriteShort(short v)
		{
			stream.WriteByte((byte)(v / 0x100));
			stream.WriteByte((byte)(v % 0x100));
		}

		void WriteInt(int v)
		{
			stream.WriteByte((byte)(v / 0x1000000));
			stream.WriteByte((byte)(v / 0x10000 & 0xFF));
			stream.WriteByte((byte)(v / 0x100 & 0xFF));
			stream.WriteByte((byte)(v % 0x100));
		}

		public void WriteMusic(MidiMusic music)
		{
			WriteHeader(music.Format, (short)music.Tracks.Count, music.DeltaTimeSpec);
			foreach (var track in music.Tracks)
				WriteTrack(track);
		}

		public void WriteHeader(short format, short tracks, short deltaTimeSpec)
		{
			stream.Write(Encoding.UTF8.GetBytes("MThd"), 0, 4);
			WriteShort(0);
			WriteShort(6);
			WriteShort(format);
			WriteShort(tracks);
			WriteShort(deltaTimeSpec);
		}

		Func<bool, MidiMessage, Stream, int> meta_event_writer;

		public Func<bool, MidiMessage, Stream, int> MetaEventWriter
		{
			get { return meta_event_writer; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");
				meta_event_writer = value;
			}
		}

		public void WriteTrack(MidiTrack track)
		{
			stream.Write(Encoding.UTF8.GetBytes("MTrk"), 0, 4);
			WriteInt(GetTrackDataSize(track));

			byte running_status = 0;

			foreach (MidiMessage e in track.Messages)
			{
				Write7BitVariableInteger(e.DeltaTime);
				switch (e.Event.EventType)
				{
					case MidiEvent.Meta:
						meta_event_writer(false, e, stream);
						break;
					case MidiEvent.SysEx1:
					case MidiEvent.SysEx2:
						stream.WriteByte(e.Event.EventType);
						Write7BitVariableInteger(e.Event.ExtraDataLength);
						stream.Write(e.Event.ExtraData, e.Event.ExtraDataOffset, e.Event.ExtraDataLength);
						break;
					default:
						if (DisableRunningStatus || e.Event.StatusByte != running_status)
							stream.WriteByte(e.Event.StatusByte);
						int len = MidiEvent.FixedDataSize(e.Event.EventType);
						stream.WriteByte(e.Event.Msb);
						if (len > 1)
							stream.WriteByte(e.Event.Lsb);
						if (len > 2)
							throw new Exception("Unexpected data size: " + len);
						break;
				}
				running_status = e.Event.StatusByte;
			}
		}

		int GetVariantLength(int value)
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(String.Format("Length must be non-negative integer: {0}", value));
			if (value == 0)
				return 1;
			int ret = 0;
			for (int x = value; x != 0; x >>= 7)
				ret++;
			return ret;
		}

		int GetTrackDataSize(MidiTrack track)
		{
			int size = 0;
			byte running_status = 0;
			foreach (MidiMessage e in track.Messages)
			{
				// delta time
				size += GetVariantLength(e.DeltaTime);

				// arguments
				switch (e.Event.EventType)
				{
					case MidiEvent.Meta:
						size += meta_event_writer(true, e, null);
						break;
					case MidiEvent.SysEx1:
					case MidiEvent.SysEx2:
						size++;
						size += GetVariantLength(e.Event.ExtraDataLength);
						size += e.Event.ExtraDataLength;
						break;
					default:
						// message type & channel
						if (DisableRunningStatus || running_status != e.Event.StatusByte)
							size++;
						size += MidiEvent.FixedDataSize(e.Event.EventType);
						break;
				}

				running_status = e.Event.StatusByte;
			}
			return size;
		}

		void Write7BitVariableInteger(int value)
		{
			Write7BitVariableInteger(value, false);
		}

		void Write7BitVariableInteger(int value, bool shifted)
		{
			if (value == 0)
			{
				stream.WriteByte((byte)(shifted ? 0x80 : 0));
				return;
			}
			if (value >= 0x80)
				Write7BitVariableInteger(value >> 7, true);
			stream.WriteByte((byte)((value & 0x7F) + (shifted ? 0x80 : 0)));
		}
	}
}
