using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Commons.Music.Midi.UwpWithStub.Commons.Music.Midi.UwpMidi {
	public class UwpMidiAccess : IMidiAccess {
		public UwpMidiAccess ()
		{
		}

		public async Task<IEnumerable<IMidiPortDetails>> GetInputsAsync ()
		{
			return DeviceInformation.FindAllAsync (MidiInPort.GetDeviceSelector ())
						.GetResults ().Select (i => new UwpMidiPortDetails (i));
		}

		public async Task<IEnumerable<IMidiPortDetails>> GetOutputsAsync ()
		{
			return DeviceInformation.FindAllAsync (MidiOutPort.GetDeviceSelector ())
						.GetResults ().Select (i => new UwpMidiPortDetails (i));
		}

		public IEnumerable<IMidiPortDetails> Inputs {
			get { return GetInputsAsync ().Result; }
		}

		public IEnumerable<IMidiPortDetails> Outputs {
			get { return GetOutputsAsync ().Result; }
		}

		public event EventHandler<MidiConnectionEventArgs> StateChanged;

		public async Task<IMidiInput> OpenInputAsync (string portId)
		{
			var inputs = await GetInputsAsync ();
			var details = inputs.Cast<UwpMidiPortDetails> ().FirstOrDefault (d => d.Id == portId);
			var input = MidiInPort.FromIdAsync (portId).GetResults ();
			return new UwpMidiInput (input, details);
		}

		public async Task<IMidiOutput> OpenOutputAsync (string portId)
		{
			var outputs = await GetOutputsAsync ();
			var details = outputs.Cast<UwpMidiPortDetails> ().FirstOrDefault (d => d.Id == portId);
			var output = MidiOutPort.FromIdAsync (portId).GetResults ();
			return new UwpMidiOutput (output, details);
		}
	}

	public class UwpMidiPortDetails : IMidiPortDetails {
		private DeviceInformation i;

		public UwpMidiPortDetails (DeviceInformation i)
		{
			this.i = i;
		}

		public DeviceInformation Device => i;

		public string Id => i.Id;

		public string Manufacturer => throw new NotImplementedException ();

		public string Name => i.Name;

		public string Version => throw new NotImplementedException ();
	}

	public class UwpMidiInput : IMidiInput {
		internal UwpMidiInput (MidiInPort input, UwpMidiPortDetails details)
		{
			this.input = input;
			Details = details;
			Connection = MidiPortConnectionState.Open;
			input.MessageReceived += DispatchMessageReceived;
		}

		MidiInPort input;

		public IMidiPortDetails Details { get; private set; }

		public MidiPortConnectionState Connection { get; private set; }

		public event EventHandler<MidiReceivedEventArgs> MessageReceived;

		void DispatchMessageReceived (MidiInPort port, MidiMessageReceivedEventArgs args)
		{
			var data = args.Message.RawData.ToArray ();
			MessageReceived (this, new MidiReceivedEventArgs { Data = data, Start = 0, Length = data.Length, Timestamp = (long)args.Message.Timestamp.TotalMilliseconds });
		}

		public async Task CloseAsync ()
		{
			Connection = MidiPortConnectionState.Pending;
			await Task.Run (() => {
				input.Dispose ();
				Connection = MidiPortConnectionState.Closed;
			});
		}

		public void Dispose ()
		{
			input.MessageReceived -= DispatchMessageReceived;
			CloseAsync ().RunSynchronously ();
		}
	}

	public class UwpMidiOutput : IMidiOutput {
		internal UwpMidiOutput (IMidiOutPort output, UwpMidiPortDetails details)
		{
			this.output = output;
			Details = details;
			Connection = MidiPortConnectionState.Open;
		}

		IMidiOutPort output;

		public IMidiPortDetails Details { get; private set; }

		public MidiPortConnectionState Connection { get; private set; }

		public async Task CloseAsync ()
		{
			Connection = MidiPortConnectionState.Pending;
			await Task.Run (() => {
				var d = output as IDisposable;
				if (d != null)
					d.Dispose ();
				Connection = MidiPortConnectionState.Closed;
			});
		}

		public void Dispose ()
		{
			CloseAsync ().RunSynchronously ();
		}

		public void Send (byte [] mevent, int offset, int length, long timestamp)
		{
			var events = Convert (mevent, offset, length, timestamp);
			foreach (var e in events)
				output.SendMessage (e);
		}

		struct Buffer : IBuffer {
			byte [] array;
			uint offset;
			uint length;

			public Buffer (byte [] array, int offset, int length)
			{
				this.array = array;
				this.offset = (uint)offset;
				this.length = (uint)length;
			}

			public uint Capacity => length;

			public uint Length { get => length; set => throw new NotSupportedException (); }
		}

		IEnumerable<IMidiMessage> Convert (byte [] mevent, int offset, int length, long timestamp)
		{
			int end = offset + length;
			for (int i = offset; i < end;) {
				switch (mevent [i] & 0xF0) {
				case MidiEvent.NoteOn:
					if (i + 3 < end)
						yield return new MidiNoteOnMessage ((byte)(mevent [i] & 0x7F), mevent [i + 1], mevent [i + 2]);
					break;
				case MidiEvent.NoteOff:
					if (i + 3 < end)
						yield return new MidiNoteOffMessage ((byte)(mevent [i] & 0x7F), mevent [i + 1], mevent [i + 2]);
					break;
				case MidiEvent.PAf:
					if (i + 3 < end)
						yield return new MidiPolyphonicKeyPressureMessage ((byte)(mevent [i] & 0x7F), mevent [i + 1], mevent [i + 2]);
					break;
				case MidiEvent.CC:
					if (i + 3 < end)
						yield return new MidiControlChangeMessage ((byte)(mevent [i] & 0x7F), mevent [i + 1], mevent [i + 2]);
					break;
				case MidiEvent.Program:
					if (i + 2 < end)
						yield return new MidiProgramChangeMessage ((byte)(mevent [i] & 0x7F), mevent [i + 1]);
					break;
				case MidiEvent.CAf:
					if (i + 2 < end)
						yield return new MidiChannelPressureMessage ((byte)(mevent [i] & 0x7F), mevent [i + 1]);
					break;
				case MidiEvent.Pitch:
					if (i + 3 < end)
						yield return new MidiPitchBendChangeMessage ((byte)(mevent [i] & 0x7F), (ushort)((mevent [i + 1] << 13) + mevent [i + 2]));
					break;
				case MidiEvent.SysEx1:
					int pos = Array.IndexOf (mevent, MidiEvent.EndSysEx, i, length - i);
					if (pos >= 0)
						yield return new MidiSystemExclusiveMessage (new Buffer (mevent, i, pos - i));
					break;
				default:
					throw new NotSupportedException ($"MIDI message byte '{mevent [i].ToString ("X02")}' is not supported.");
				}
				if (mevent [i] != MidiEvent.SysEx1)
					i += MidiEvent.FixedDataSize (mevent [i]);
			}
		}
	}
}
