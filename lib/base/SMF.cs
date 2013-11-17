using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Commons.Music.Midi
{
	public class SmfMusic
	{
		List<SmfTrack> tracks = new List<SmfTrack> ();

		public SmfMusic ()
		{
			Format = 1;
		}

		public short DeltaTimeSpec { get; set; }

		public byte Format { get; set; }

		public void AddTrack (SmfTrack track)
		{
			this.tracks.Add (track);
		}

		public IList<SmfTrack> Tracks {
			get { return tracks; }
		}

		public int GetTotalPlayTimeMilliseconds ()
		{
			if (Format != 0)
				throw new NotSupportedException ("Format 1 is not suitable to compute total play time within a song");
			return GetTotalPlayTimeMilliseconds (Tracks [0].Events, DeltaTimeSpec);
		}
		
		public static int GetTotalPlayTimeMilliseconds (IList<SmfEvent> events, int deltaTimeSpec)
		{
			if (deltaTimeSpec < 0)
				throw new NotSupportedException ("non-tick based DeltaTime");
			else {
				int tempo = SmfMetaType.DefaultTempo;
				int v = 0;
				foreach (var e in events) {
					v += (int) (tempo / 1000 * e.DeltaTime / deltaTimeSpec);
					if (e.Message.MessageType == SmfMessage.Meta && e.Message.Msb == SmfMetaType.Tempo)
						tempo = SmfMetaType.GetTempo (e.Message.Data);
				}
				return v;
			}
		}
	}

	public class SmfTrack
	{
		public SmfTrack ()
			: this (new List<SmfEvent> ())
		{
		}

		public SmfTrack (IList<SmfEvent> events)
		{
			if (events == null)
				throw new ArgumentNullException ("events");
			this.events = events as List<SmfEvent> ?? new List<SmfEvent> (events);
		}

		List<SmfEvent> events;

		public void AddEvent (SmfEvent evt)
		{
			events.Add (evt);
		}

		public IList<SmfEvent> Events {
			get { return events; }
		}
	}

	public struct SmfEvent
	{
		public SmfEvent (int deltaTime, SmfMessage msg)
		{
			DeltaTime = deltaTime;
			Message = msg;
		}

		public readonly int DeltaTime;
		public readonly SmfMessage Message;

		public override string ToString ()
		{
			return String.Format ("[dt{0}]{1}", DeltaTime, Message);
		}
	}

	public static class SmfCC
	{
		public const byte BankSelect = 0x00;
		public const byte Modulation = 0x01;
		public const byte Breath = 0x02;
		public const byte Foot = 0x04;
		public const byte PortamentTime = 0x05;
		public const byte DteMsb = 0x06;
		public const byte Volume = 0x07;
		public const byte Balance = 0x08;
		public const byte Pan = 0x0A;
		public const byte Expression = 0x0B;
		public const byte EffectControl1 = 0x0C;
		public const byte EffectControl2 = 0x0D;
		public const byte General1 = 0x10;
		public const byte General2 = 0x11;
		public const byte General3 = 0x12;
		public const byte General4 = 0x13;
		public const byte BankSelectLsb = 0x20;
		public const byte ModulationLsb = 0x21;
		public const byte BreathLsb = 0x22;
		public const byte FootLsb = 0x24;
		public const byte PortamentTimeLsb = 0x25;
		public const byte DteLsb = 0x26;
		public const byte VolumeLsb = 0x27;
		public const byte BalanceLsb = 0x28;
		public const byte PanLsb = 0x2A;
		public const byte ExpressionLsb = 0x2B;
		public const byte Effect1Lsb = 0x2C;
		public const byte Effect2Lsb = 0x2D;
		public const byte General1Lsb = 0x30;
		public const byte General2Lsb = 0x31;
		public const byte General3Lsb = 0x32;
		public const byte General4Lsb = 0x33;
		public const byte Hold = 0x40;
		public const byte PortamentoSwitch = 0x41;
		public const byte Sostenuto = 0x42;
		public const byte SoftPedal = 0x43;
		public const byte Legato = 0x44;
		public const byte Hold2 = 0x45;
		public const byte SoundController1 = 0x46;
		public const byte SoundController2 = 0x47;
		public const byte SoundController3 = 0x48;
		public const byte SoundController4 = 0x49;
		public const byte SoundController5 = 0x4A;
		public const byte SoundController6 = 0x4B;
		public const byte SoundController7 = 0x4C;
		public const byte SoundController8 = 0x4D;
		public const byte SoundController9 = 0x4E;
		public const byte SoundController10 = 0x4F;
		public const byte General5 = 0x50;
		public const byte General6 = 0x51;
		public const byte General7 = 0x52;
		public const byte General8 = 0x53;
		public const byte PortamentoControl = 0x54;
		public const byte Rsd = 0x5B;
		public const byte Effect1 = 0x5B;
		public const byte Tremolo = 0x5C;
		public const byte Effect2 = 0x5C;
		public const byte Csd = 0x5D;
		public const byte Effect3 = 0x5D;
		public const byte Celeste = 0x5E;
		public const byte Effect4 = 0x5E;
		public const byte Phaser = 0x5F;
		public const byte Effect5 = 0x5F;
		public const byte DteIncrement = 0x60;
		public const byte DteDecrement = 0x61;
		public const byte NrpnLsb = 0x62;
		public const byte NrpnMsb = 0x63;
		public const byte RpnLsb = 0x64;
		public const byte RpnMsb = 0x65;
		// Channel mode messages
		public const byte AllSoundOff = 0x78;
		public const byte ResetAllControllers = 0x79;
		public const byte LocalControl = 0x7A;
		public const byte AllNotesOff = 0x7B;
		public const byte OmniModeOff = 0x7C;
		public const byte OmniModeOn = 0x7D;
		public const byte PolyModeOnOff = 0x7E;
		public const byte PolyModeOn = 0x7F;
	}
	
	public static class SmfRpnType
	{
		public const short PitchBendSensitivity = 0;
		public const short FineTuning = 1;
		public const short CoarseTuning = 2;
		public const short TuningProgram = 3;
		public const short TuningBankSelect = 4;
		public const short ModulationDepth = 5;
	}
	
	public static class SmfMetaType
	{
		public const byte SequenceNumber = 0x00;
		public const byte Text = 0x01;
		public const byte Copyright = 0x02;
		public const byte TrackName = 0x03;
		public const byte InstrumentName = 0x04;
		public const byte Lyric = 0x05;
		public const byte Marker = 0x06;
		public const byte Cue = 0x07;
		public const byte ChannelPrefix = 0x20;
		public const byte EndOfTrack = 0x2F;
		public const byte Tempo = 0x51;
		public const byte SmpteOffset = 0x54;
		public const byte TimeSignature = 0x58;
		public const byte KeySignature = 0x59;
		public const byte SequencerSpecific = 0x7F;

		public const int DefaultTempo = 500000;
		
		public static int GetTempo (byte [] data)
		{
			return (data [0] << 16) + (data [1] << 8) + data [2];
		}
	}

	public struct SmfMessage
	{
		public const byte NoteOff = 0x80;
		public const byte NoteOn = 0x90;
		public const byte PAf = 0xA0;
		public const byte CC = 0xB0;
		public const byte Program = 0xC0;
		public const byte CAf = 0xD0;
		public const byte Pitch = 0xE0;
		public const byte SysEx1 = 0xF0;
		public const byte SysEx2 = 0xF7;
		public const byte Meta = 0xFF;

		public const byte EndSysEx = 0xF7;

		public SmfMessage (int value)
		{
			Value = value;
			Data = null;
		}

		public SmfMessage (byte type, byte arg1, byte arg2, byte [] data)
		{
			Value = type + (arg1 << 8) + (arg2 << 16);
			Data = data;
		}

		public readonly int Value;

		// This expects EndSysEx byte _inclusive_ for F0 message.
		public readonly byte [] Data;

		public byte StatusByte {
			get { return (byte) (Value & 0xFF); }
		}

		public byte MessageType {
			get {
				switch (StatusByte) {
				case Meta:
				case SysEx1:
				case SysEx2:
					return StatusByte;
				default:
					return (byte) (Value & 0xF0);
				}
			}
		}

		public byte Msb {
			get { return (byte) ((Value & 0xFF00) >> 8); }
		}

		public byte Lsb {
			get { return (byte) ((Value & 0xFF0000) >> 16); }
		}

		public byte MetaType {
			get { return Msb; }
		}

		public byte Channel {
			get { return (byte) (Value & 0x0F); }
		}

		public static byte FixedDataSize (byte statusByte)
		{
			switch (statusByte & 0xF0) {
			case 0xF0: // and 0xF7, 0xFF
				return 0; // no fixed data
			case Program: // ProgramChg
			case CAf: // CAf
				return 1;
			default:
				return 2;
			}
		}

		public override string ToString ()
		{
			return String.Format ("{0:X02}:{1:X02}:{2:X02}{3}", StatusByte, Msb, Lsb, Data != null ? Data.Length + "[data]" : "");
		}
	}

	public class SmfWriter
	{
		Stream stream;

		public SmfWriter (Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");
			this.stream = stream;

			// default meta event writer.
			meta_event_writer = SmfWriterExtension.DefaultMetaEventWriter;
		}

		public bool DisableRunningStatus { get; set; }

		void WriteShort (short v)
		{
			stream.WriteByte ((byte) (v / 0x100));
			stream.WriteByte ((byte) (v % 0x100));
		}

		void WriteInt (int v)
		{
			stream.WriteByte ((byte) (v / 0x1000000));
			stream.WriteByte ((byte) (v / 0x10000 & 0xFF));
			stream.WriteByte ((byte) (v / 0x100 & 0xFF));
			stream.WriteByte ((byte) (v % 0x100));
		}

		public void WriteMusic (SmfMusic music)
		{
			WriteHeader (music.Format, (short) music.Tracks.Count, music.DeltaTimeSpec);
			foreach (var track in music.Tracks)
				WriteTrack (track);
		}

		public void WriteHeader (short format, short tracks, short deltaTimeSpec)
		{
			stream.Write (Encoding.UTF8.GetBytes ("MThd"), 0, 4);
			WriteShort (0);
			WriteShort (6);
			WriteShort (format);
			WriteShort (tracks);
			WriteShort (deltaTimeSpec);
		}

		Func<bool,SmfEvent,Stream,int> meta_event_writer;

		public Func<bool,SmfEvent,Stream,int> MetaEventWriter {
			get { return meta_event_writer; }
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				meta_event_writer = value;
			}
		}

		public void WriteTrack (SmfTrack track)
		{
			stream.Write (Encoding.UTF8.GetBytes ("MTrk"), 0, 4);
			WriteInt (GetTrackDataSize (track));

			byte running_status = 0;

			foreach (SmfEvent e in track.Events) {
				Write7BitVariableInteger (e.DeltaTime);
				switch (e.Message.MessageType) {
				case SmfMessage.Meta:
					meta_event_writer (false, e, stream);
					break;
				case SmfMessage.SysEx1:
				case SmfMessage.SysEx2:
					stream.WriteByte (e.Message.MessageType);
					Write7BitVariableInteger (e.Message.Data.Length);
					stream.Write (e.Message.Data, 0, e.Message.Data.Length);
					break;
				default:
					if (DisableRunningStatus || e.Message.StatusByte != running_status)
						stream.WriteByte (e.Message.StatusByte);
					int len = SmfMessage.FixedDataSize (e.Message.MessageType);
					stream.WriteByte (e.Message.Msb);
					if (len > 1)
						stream.WriteByte (e.Message.Lsb);
					if (len > 2)
						throw new Exception ("Unexpected data size: " + len);
					break;
				}
				running_status = e.Message.StatusByte;
			}
		}

		int GetVariantLength (int value)
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException (String.Format ("Length must be non-negative integer: {0}", value));
			if (value == 0)
				return 1;
			int ret = 0;
			for (int x = value; x != 0; x >>= 7)
				ret++;
			return ret;
		}

		int GetTrackDataSize (SmfTrack track)
		{
			int size = 0;
			byte running_status = 0;
			foreach (SmfEvent e in track.Events) {
				// delta time
				size += GetVariantLength (e.DeltaTime);

				// arguments
				switch (e.Message.MessageType) {
				case SmfMessage.Meta:
					size += meta_event_writer (true, e, null);
					break;
				case SmfMessage.SysEx1:
				case SmfMessage.SysEx2:
					size++;
					size += GetVariantLength (e.Message.Data.Length);
					size += e.Message.Data.Length;
					break;
				default:
					// message type & channel
					if (DisableRunningStatus || running_status != e.Message.StatusByte)
						size++;
					size += SmfMessage.FixedDataSize (e.Message.MessageType);
					break;
				}

				running_status = e.Message.StatusByte;
			}
			return size;
		}

		void Write7BitVariableInteger (int value)
		{
			Write7BitVariableInteger (value, false);
		}

		void Write7BitVariableInteger (int value, bool shifted)
		{
			if (value == 0) {
				stream.WriteByte ((byte) (shifted ? 0x80 : 0));
				return;
			}
			if (value >= 0x80)
				Write7BitVariableInteger (value >> 7, true);
			stream.WriteByte ((byte) ((value & 0x7F) + (shifted ? 0x80 : 0)));
		}
	}

	public static class SmfWriterExtension
	{

		static readonly Func<bool, SmfEvent, Stream, int> default_meta_writer, vsq_meta_text_splitter;

		static SmfWriterExtension ()
		{
			default_meta_writer = delegate (bool lengthMode, SmfEvent e, Stream stream) {
				if (lengthMode) {
					if (e.Message.Data.Length == 0)
						return 3; // 0xFF, metaType, 0

					// [0x00] 0xFF metaType size ... (note that for more than one meta event it requires step count of 0).
					int repeatCount = e.Message.Data.Length / 0x7F;
					if (repeatCount == 0)
						return 3 + e.Message.Data.Length;
					int mod = e.Message.Data.Length % 0x7F;
					return repeatCount * (4 + 0x7F) - 1 + (mod > 0 ? 4 + mod : 0);
				}

				int written = 0;
				int total = e.Message.Data.Length;
				do {
					if (written > 0)
						stream.WriteByte (0); // step
					stream.WriteByte (0xFF);
					stream.WriteByte (e.Message.MetaType);
					int size = Math.Min (0x7F, total - written);
					stream.WriteByte ((byte) size);
					stream.Write (e.Message.Data, written, size);
					written += size;
				} while (written < total);
				return 0;
			};

			vsq_meta_text_splitter = delegate (bool lengthMode, SmfEvent e, Stream stream) {
				// The split should not be applied to "Master Track"
				if (e.Message.Data.Length < 0x80) {
					return default_meta_writer (lengthMode, e, stream);
				}

				if (lengthMode) {
					if (e.Message.Data.Length == 0)
						return 11; // 0xFF, metaType, 8, "DM:0000:"

					// { [0x00] 0xFF metaType DM:xxxx:... } * repeat + 0x00 0xFF metaType DM:xxxx:mod... 
					// (note that for more than one meta event it requires step count of 0).
					int repeatCount = e.Message.Data.Length / 0x77;
					int mod = e.Message.Data.Length % 0x77;
					return repeatCount * (12 + 0x77) - (repeatCount > 0 ? 1 : 0) + (mod > 0 ? 12 + mod : 0);
				}


				int written = 0;
				int total = e.Message.Data.Length;
				int idx = 0;
				do {
					if (written > 0)
						stream.WriteByte (0); // step
					stream.WriteByte (0xFF);
					stream.WriteByte (e.Message.MetaType);
					int size = Math.Min (0x77, total - written);
					stream.WriteByte ((byte) (size + 8));
					stream.Write (Encoding.ASCII.GetBytes (String.Format ("DM:{0:D04}:", idx++)), 0, 8);
					stream.Write (e.Message.Data, written, size);
					written += size;
				} while (written < total);
				return 0;
			};
		}

		public static Func<bool, SmfEvent, Stream, int> DefaultMetaEventWriter {
			get { return default_meta_writer; }
		}

		public static Func<bool, SmfEvent, Stream, int> VsqMetaTextSplitter {
			get { return vsq_meta_text_splitter; }
		}
	}

	public class SmfReader
	{
		public SmfReader (Stream stream)
		{
			this.stream = stream;
		}

		Stream stream;
		SmfMusic data = new SmfMusic ();

		public SmfMusic Music { get { return data; } }

		public void Parse ()
		{
			if (
			    ReadByte ()  != 'M'
			    || ReadByte ()  != 'T'
			    || ReadByte ()  != 'h'
			    || ReadByte ()  != 'd')
				throw ParseError ("MThd is expected");
			if (ReadInt32 () != 6)
				throw ParseError ("Unexpeted data size (should be 6)");
			data.Format = (byte) ReadInt16 ();
			int tracks = ReadInt16 ();
			data.DeltaTimeSpec = ReadInt16 ();
			try {
				for (int i = 0; i < tracks; i++)
					data.Tracks.Add (ReadTrack ());
			} catch (FormatException ex) {
				throw ParseError ("Unexpected data error", ex);
			}
		}

		SmfTrack ReadTrack ()
		{
			var tr = new SmfTrack ();
			if (
			    ReadByte ()  != 'M'
			    || ReadByte ()  != 'T'
			    || ReadByte ()  != 'r'
			    || ReadByte ()  != 'k')
				throw ParseError ("MTrk is expected");
			int trackSize = ReadInt32 ();
			current_track_size = 0;
			int total = 0;
			while (current_track_size < trackSize) {
				int delta = ReadVariableLength ();
				tr.Events.Add (ReadEvent (delta));
				total += delta;
			}
			if (current_track_size != trackSize)
				throw ParseError ("Size information mismatch");
			return tr;
		}

		int current_track_size;
		byte running_status;

		SmfEvent ReadEvent (int deltaTime)
		{
			byte b = PeekByte ();
			running_status = b < 0x80 ? running_status : ReadByte ();
			int len;
			switch (running_status) {
			case SmfMessage.SysEx1:
			case SmfMessage.SysEx2:
			case SmfMessage.Meta:
				byte metaType = running_status == SmfMessage.Meta ? ReadByte () : (byte) 0;
				len = ReadVariableLength ();
				byte [] args = new byte [len];
				if (len > 0)
					ReadBytes (args);
				return new SmfEvent (deltaTime, new SmfMessage (running_status, metaType, 0, args));
			default:
				int value = running_status;
				value += ReadByte () << 8;
				if (SmfMessage.FixedDataSize (running_status) == 2)
					value += ReadByte () << 16;
				return new SmfEvent (deltaTime, new SmfMessage (value));
			}
		}

		void ReadBytes (byte [] args)
		{
			current_track_size += args.Length;
			int start = 0;
			if (peek_byte >= 0) {
				args [0] = (byte) peek_byte;
				peek_byte = -1;
				start = 1;
			}
			int len = stream.Read (args, start, args.Length - start);
			try {
			if (len < args.Length - start)
				throw ParseError (String.Format ("The stream is insufficient to read {0} bytes specified in the SMF event. Only {1} bytes read.", args.Length, len));
			} finally {
				stream_position += len;
			}
		}

		int ReadVariableLength ()
		{
			int val = 0;
			for (int i = 0; i < 4; i++) {
				byte b = ReadByte ();
				val = (val << 7) + b;
				if (b < 0x80)
					return val;
				val -= 0x80;
			}
			throw ParseError ("Delta time specification exceeds the 4-byte limitation.");
		}

		int peek_byte = -1;
		int stream_position;

		byte PeekByte ()
		{
			if (peek_byte < 0)
				peek_byte = stream.ReadByte ();
			if (peek_byte < 0)
				throw ParseError ("Insufficient stream. Failed to read a byte.");
			return (byte) peek_byte;
		}

		byte ReadByte ()
		{
			try {

			current_track_size++;
			if (peek_byte >= 0) {
				byte b = (byte) peek_byte;
				peek_byte = -1;
				return b;
			}
			int ret = stream.ReadByte ();
			if (ret < 0)
				throw ParseError ("Insufficient stream. Failed to read a byte.");
			return (byte) ret;

			} finally {
				stream_position++;
			}
		}

		short ReadInt16 ()
		{
			return (short) ((ReadByte () << 8) + ReadByte ());
		}

		int ReadInt32 ()
		{
			return (((ReadByte () << 8) + ReadByte () << 8) + ReadByte () << 8) + ReadByte ();
		}

		Exception ParseError (string msg)
		{
			return ParseError (msg, null);
		}

		Exception ParseError (string msg, Exception innerException)
		{
			throw new SmfParserException (String.Format (msg + "(at {0})", stream_position), innerException);
		}
	}

	public class SmfParserException : Exception
	{
		public SmfParserException () : this ("SMF parser error") {}
		public SmfParserException (string message) : base (message) {}
		public SmfParserException (string message, Exception innerException) : base (message, innerException) {}
	}

	public class SmfTrackMerger
	{
		public static SmfMusic Merge (SmfMusic source)
		{
			return new SmfTrackMerger (source).GetMergedEvents ();
		}

		SmfTrackMerger (SmfMusic source)
		{
			this.source = source;
		}

		SmfMusic source;

		// FIXME: it should rather be implemented to iterate all
		// tracks with index to events, pick the track which contains
		// the nearest event and push the events into the merged queue.
		// It's simpler, and costs less by removing sort operation
		// over thousands of events.
		SmfMusic GetMergedEvents ()
		{
			IList<SmfEvent> l = new List<SmfEvent> ();

			foreach (var track in source.Tracks) {
				int delta = 0;
				foreach (var mev in track.Events) {
					delta += mev.DeltaTime;
					l.Add (new SmfEvent (delta, mev.Message));
				}
			}

			if (l.Count == 0)
				return new SmfMusic () { DeltaTimeSpec = source.DeltaTimeSpec }; // empty (why did you need to sort your song file?)

			// Sort() does not always work as expected.
			// For example, it does not always preserve event 
			// orders on the same channels when the delta time
			// of event B after event A is 0. It could be sorted
			// either as A->B or B->A.
			//
			// To resolve this ieeue, we have to sort "chunk"
			// of events, not all single events themselves, so
			// that order of events in the same chunk is preserved
			// i.e. [AB] at 48 and [CDE] at 0 should be sorted as
			// [CDE] [AB].

			var idxl = new List<int> (l.Count);
			idxl.Add (0);
			int prev = 0;
			for (int i = 0; i < l.Count; i++) {
				if (l [i].DeltaTime != prev) {
					idxl.Add (i);
					prev = l [i].DeltaTime;
				}
			}

			idxl.Sort (delegate (int i1, int i2) {
				return l [i1].DeltaTime - l [i2].DeltaTime;
				});

			// now build a new event list based on the sorted blocks.
			var l2 = new List<SmfEvent> (l.Count);
			int idx;
			for (int i = 0; i < idxl.Count; i++)
				for (idx = idxl [i], prev = l [idx].DeltaTime; idx < l.Count && l [idx].DeltaTime == prev; idx++)
					l2.Add (l [idx]);
//if (l.Count != l2.Count) throw new Exception (String.Format ("Internal eror: count mismatch: l1 {0} l2 {1}", l.Count, l2.Count));
			l = l2;

			// now events should be sorted correctly.

			var waitToNext = l [0].DeltaTime;
			for (int i = 0; i < l.Count - 1; i++) {
				if (l [i].Message.Value != 0) { // if non-dummy
					var tmp = l [i + 1].DeltaTime - l [i].DeltaTime;
					l [i] = new SmfEvent (waitToNext, l [i].Message);
					waitToNext = tmp;
				}
			}
			l [l.Count - 1] = new SmfEvent (waitToNext, l [l.Count - 1].Message);

			var m = new SmfMusic ();
			m.DeltaTimeSpec = source.DeltaTimeSpec;
			m.Format = 0;
			m.Tracks.Add (new SmfTrack (l));
			return m;
		}
	}

	public class SmfTrackSplitter
	{
		public static SmfMusic Split (IList<SmfEvent> source, short deltaTimeSpec)
		{
			return new SmfTrackSplitter (source, deltaTimeSpec).Split ();
		}

		SmfTrackSplitter (IList<SmfEvent> source, short deltaTimeSpec)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			this.source = source;
			delta_time_spec = deltaTimeSpec;
			var mtr = new SplitTrack (-1);
			tracks.Add (-1, mtr);
		}

		IList<SmfEvent> source;
		short delta_time_spec;
		Dictionary<int,SplitTrack> tracks = new Dictionary<int,SplitTrack> ();

		class SplitTrack
		{
			public SplitTrack (int trackID)
			{
				TrackID = trackID;
				Track = new SmfTrack ();
			}

			public int TrackID;
			public int TotalDeltaTime;
			public SmfTrack Track;

			public void AddEvent (int deltaInsertAt, SmfEvent e)
			{
				e = new SmfEvent (deltaInsertAt - TotalDeltaTime, e.Message);
				Track.Events.Add (e);
				TotalDeltaTime = deltaInsertAt;
			}
		}

		SplitTrack GetTrack (int track)
		{
			SplitTrack t;
			if (!tracks.TryGetValue (track, out t)) {
				t = new SplitTrack (track);
				tracks [track] = t;
			}
			return t;
		}

		// Override it to customize track dispatcher. It would be
		// useful to split note messages out from non-note ones,
		// to ease data reading.
		public virtual int GetTrackID (SmfEvent e)
		{
			switch (e.Message.MessageType) {
			case SmfMessage.Meta:
			case SmfMessage.SysEx1:
			case SmfMessage.SysEx2:
				return -1;
			default:
				return e.Message.Channel;
			}
		}

		public SmfMusic Split ()
		{
			int totalDeltaTime = 0;
			foreach (var e in source) {
				totalDeltaTime += e.DeltaTime;
				int id = GetTrackID (e);
				GetTrack (id).AddEvent (totalDeltaTime, e);
			}

			var m = new SmfMusic ();
			m.DeltaTimeSpec = delta_time_spec;
			foreach (var t in tracks.Values)
				m.Tracks.Add (t.Track);
			return m;
		}
	}
}
