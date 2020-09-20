using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Commons.Music.Midi
{
	public class SmfReader
	{
		Stream stream;
		MidiMusic data;

		public MidiMusic Music { get { return data; } }

		public void Read(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			this.stream = stream;
			data = new MidiMusic();
			try
			{
				DoParse();
			}
			finally
			{
				this.stream = null;
			}
		}

		void DoParse()
		{
			if (
				ReadByte() != 'M'
				|| ReadByte() != 'T'
				|| ReadByte() != 'h'
				|| ReadByte() != 'd')
				throw ParseError("MThd is expected");
			if (ReadInt32() != 6)
				throw ParseError("Unexpected data size (should be 6)");
			data.Format = (byte)ReadInt16();
			int tracks = ReadInt16();
			data.DeltaTimeSpec = ReadInt16();
			try
			{
				for (int i = 0; i < tracks; i++)
					data.Tracks.Add(ReadTrack());
			}
			catch (FormatException ex)
			{
				throw ParseError("Unexpected data error", ex);
			}
		}

		MidiTrack ReadTrack()
		{
			var tr = new MidiTrack();
			if (
				ReadByte() != 'M'
				|| ReadByte() != 'T'
				|| ReadByte() != 'r'
				|| ReadByte() != 'k')
				throw ParseError("MTrk is expected");
			int trackSize = ReadInt32();
			current_track_size = 0;
			int total = 0;
			while (current_track_size < trackSize)
			{
				int delta = ReadVariableLength();
				tr.Messages.Add(ReadMessage(delta));
				total += delta;
			}
			if (current_track_size != trackSize)
				throw ParseError("Size information mismatch");
			return tr;
		}

		int current_track_size;
		byte running_status;

		MidiMessage ReadMessage(int deltaTime)
		{
			byte b = PeekByte();
			running_status = b < 0x80 ? running_status : ReadByte();
			int len;
			switch (running_status)
			{
				case MidiEvent.SysEx1:
				case MidiEvent.SysEx2:
				case MidiEvent.Meta:
					byte metaType = running_status == MidiEvent.Meta ? ReadByte() : (byte)0;
					len = ReadVariableLength();
					byte[] args = new byte[len];
					if (len > 0)
						ReadBytes(args);
					return new MidiMessage(deltaTime, new MidiEvent(running_status, metaType, 0, args, 0, args.Length));
				default:
					int value = running_status;
					value += ReadByte() << 8;
					if (MidiEvent.FixedDataSize(running_status) == 2)
						value += ReadByte() << 16;
					return new MidiMessage(deltaTime, new MidiEvent(value));
			}
		}

		void ReadBytes(byte[] args)
		{
			current_track_size += args.Length;
			int start = 0;
			if (peek_byte >= 0)
			{
				args[0] = (byte)peek_byte;
				peek_byte = -1;
				start = 1;
			}
			int len = stream.Read(args, start, args.Length - start);
			try
			{
				if (len < args.Length - start)
					throw ParseError(String.Format("The stream is insufficient to read {0} bytes specified in the SMF message. Only {1} bytes read.", args.Length, len));
			}
			finally
			{
				stream_position += len;
			}
		}

		int ReadVariableLength()
		{
			int val = 0;
			for (int i = 0; i < 4; i++)
			{
				byte b = ReadByte();
				val = (val << 7) + b;
				if (b < 0x80)
					return val;
				val -= 0x80;
			}
			throw ParseError("Delta time specification exceeds the 4-byte limitation.");
		}

		int peek_byte = -1;
		int stream_position;

		byte PeekByte()
		{
			if (peek_byte < 0)
				peek_byte = stream.ReadByte();
			if (peek_byte < 0)
				throw ParseError("Insufficient stream. Failed to read a byte.");
			return (byte)peek_byte;
		}

		byte ReadByte()
		{
			try
			{

				current_track_size++;
				if (peek_byte >= 0)
				{
					byte b = (byte)peek_byte;
					peek_byte = -1;
					return b;
				}
				int ret = stream.ReadByte();
				if (ret < 0)
					throw ParseError("Insufficient stream. Failed to read a byte.");
				return (byte)ret;

			}
			finally
			{
				stream_position++;
			}
		}

		short ReadInt16()
		{
			return (short)((ReadByte() << 8) + ReadByte());
		}

		int ReadInt32()
		{
			return (((ReadByte() << 8) + ReadByte() << 8) + ReadByte() << 8) + ReadByte();
		}

		Exception ParseError(string msg)
		{
			return ParseError(msg, null);
		}

		Exception ParseError(string msg, Exception innerException)
		{
			throw new SmfParserException(String.Format(msg + "(at {0})", stream_position), innerException);
		}
	}
}
