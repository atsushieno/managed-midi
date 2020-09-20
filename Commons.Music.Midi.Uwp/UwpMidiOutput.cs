using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Midi;
using Windows.Storage.Streams;

namespace Commons.Music.Midi.Uwp
{
	public class UwpMidiOutput : IMidiOutput
	{
		internal UwpMidiOutput(IMidiOutPort output, UwpMidiPortDetails details)
		{
			this.output = output;
			Details = details;
			Connection = MidiPortConnectionState.Open;
		}

		IMidiOutPort output;

		public IMidiPortDetails Details { get; private set; }

		public MidiPortConnectionState Connection { get; private set; }

		public async Task CloseAsync()
		{
			Connection = MidiPortConnectionState.Pending;
			await Task.Run(() => {
				var d = output as IDisposable;
				if (d != null)
					d.Dispose();
				Connection = MidiPortConnectionState.Closed;
			});
		}

		public void Dispose()
		{
			CloseAsync().Wait();
		}

		public void Send(byte[] mevent, int offset, int length, long timestamp)
		{
			var events = Convert(mevent, offset, length, timestamp);
			foreach (var e in events)
				output.SendMessage(e);
		}

		struct Buffer : IBuffer
		{
			byte[] array;
			uint offset;
			uint length;

			public Buffer(byte[] array, int offset, int length)
			{
				this.array = array;
				this.offset = (uint)offset;
				this.length = (uint)length;
			}

			public uint Capacity => length;

			public uint Length { get => length; set => throw new NotSupportedException(); }
		}

		IEnumerable<IMidiMessage> Convert(byte[] mevent, int offset, int length, long timestamp)
		{
			int end = offset + length;
			for (int i = offset; i < end;)
			{
				switch (mevent[i] & 0xF0)
				{
					case MidiEvent.ActiveSense:
						yield return new MidiActiveSensingMessage();
						break;
					case MidiEvent.NoteOn:
						if (i + 2 < end)
							yield return new MidiNoteOnMessage((byte)(mevent[i] & 0x0F), mevent[i + 1], mevent[i + 2]);
						break;
					case MidiEvent.NoteOff:
						if (i + 2 < end)
							yield return new MidiNoteOffMessage((byte)(mevent[i] & 0x0f), mevent[i + 1], mevent[i + 2]);
						break;
					case MidiEvent.PAf:
						if (i + 2 < end)
							yield return new MidiPolyphonicKeyPressureMessage((byte)(mevent[i] & 0x0f), mevent[i + 1], mevent[i + 2]);
						break;
					case MidiEvent.CC:
						if (i + 2 < end)
							yield return new MidiControlChangeMessage((byte)(mevent[i] & 0x0f), mevent[i + 1], mevent[i + 2]);
						break;
					case MidiEvent.Program:
						if (i + 2 < end)
							yield return new MidiProgramChangeMessage((byte)(mevent[i] & 0x0f), mevent[i + 1]);
						break;
					case MidiEvent.CAf:
						if (i + 2 < end)
							yield return new MidiChannelPressureMessage((byte)(mevent[i] & 0x0f), mevent[i + 1]);
						break;
					case MidiEvent.Pitch:
						if (i + 2 < end)
							yield return new MidiPitchBendChangeMessage((byte)(mevent[i] & 0x0f), (ushort)((mevent[i + 1] << 13) + mevent[i + 2]));
						break;
					case MidiEvent.SysEx1:
						int pos = Array.IndexOf(mevent, MidiEvent.EndSysEx, i, length - i);
						if (pos >= 0)
						{
							pos++;
							yield return new MidiSystemExclusiveMessage(new Buffer(mevent, i, pos - i));
							i = pos;
						}
						break;
					default:
						throw new NotSupportedException($"MIDI message byte '{mevent[i].ToString("X02")}' is not supported.");
				}
				if (mevent[i] != MidiEvent.SysEx1)
				{
					i += MidiEvent.FixedDataSize(mevent[i]) + 1;					
				}
			}
		}
	}
}
