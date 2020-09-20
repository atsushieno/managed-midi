using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Commons.Music.Midi
{
	public static class SmfWriterExtension
	{

		static readonly Func<bool, MidiMessage, Stream, int> default_meta_writer, vsq_meta_text_splitter;

		static SmfWriterExtension()
		{
			default_meta_writer = delegate (bool lengthMode, MidiMessage e, Stream stream) {
				if (lengthMode)
				{
					// [0x00] 0xFF metaType size ... (note that for more than one meta event it requires step count of 0).
					int repeatCount = e.Event.ExtraDataLength / 0x7F;
					if (repeatCount == 0)
						return 3 + e.Event.ExtraDataLength;
					int mod = e.Event.ExtraDataLength % 0x7F;
					return repeatCount * (4 + 0x7F) - 1 + (mod > 0 ? 4 + mod : 0);
				}

				int written = 0;
				int total = e.Event.ExtraDataLength;
				do
				{
					if (written > 0)
						stream.WriteByte(0); // step
					stream.WriteByte(0xFF);
					stream.WriteByte(e.Event.MetaType);
					int size = Math.Min(0x7F, total - written);
					stream.WriteByte((byte)size);
					stream.Write(e.Event.ExtraData, e.Event.ExtraDataOffset + written, size);
					written += size;
				} while (written < total);
				return 0;
			};

			vsq_meta_text_splitter = delegate (bool lengthMode, MidiMessage e, Stream stream) {
				// The split should not be applied to "Master Track"
				if (e.Event.ExtraDataLength < 0x80)
				{
					return default_meta_writer(lengthMode, e, stream);
				}

				if (lengthMode)
				{
					// { [0x00] 0xFF metaType DM:xxxx:... } * repeat + 0x00 0xFF metaType DM:xxxx:mod... 
					// (note that for more than one meta event it requires step count of 0).
					int repeatCount = e.Event.ExtraDataLength / 0x77;
					if (repeatCount == 0)
						return 11 + e.Event.ExtraDataLength;
					int mod = e.Event.ExtraDataLength % 0x77;
					return repeatCount * (12 + 0x77) - 1 + (mod > 0 ? 12 + mod : 0);
				}


				int written = 0;
				int total = e.Event.ExtraDataLength;
				int idx = 0;
				do
				{
					if (written > 0)
						stream.WriteByte(0); // step
					stream.WriteByte(0xFF);
					stream.WriteByte(e.Event.MetaType);
					int size = Math.Min(0x77, total - written);
					stream.WriteByte((byte)(size + 8));
					stream.Write(Encoding.UTF8.GetBytes(String.Format("DM:{0:D04}:", idx++)), 0, 8);
					stream.Write(e.Event.ExtraData, e.Event.ExtraDataOffset + written, size);
					written += size;
				} while (written < total);
				return 0;
			};
		}

		public static Func<bool, MidiMessage, Stream, int> DefaultMetaEventWriter
		{
			get { return default_meta_writer; }
		}

		public static Func<bool, MidiMessage, Stream, int> VsqMetaTextSplitter
		{
			get { return vsq_meta_text_splitter; }
		}
	}
}
