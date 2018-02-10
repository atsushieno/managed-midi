using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Commons.Music.Midi.WinMM
{
	public class WinMMMidiAccess : IMidiAccess
	{
		public IEnumerable<IMidiPortDetails> Inputs
		{
			get
			{
				int devs = WinMMNatives.midiInGetNumDevs();
				for (uint i = 0; i < devs; i++)
				{
					MidiInCaps caps;
					WinMMNatives.midiInGetDevCaps ((UIntPtr) i, out caps, (uint) Marshal.SizeOf (typeof (MidiInCaps)));
					yield return new WinMMPortDetails (i, caps.Name, caps.DriverVersion);
				}
			}
		}

		public IEnumerable<IMidiPortDetails> Outputs {
			get {
				int devs = WinMMNatives.midiOutGetNumDevs ();
				for (uint i = 0; i < devs; i++) {
					MidiOutCaps caps;
					var err = WinMMNatives.midiOutGetDevCaps ((UIntPtr) i, out caps, (uint) Marshal.SizeOf (typeof (MidiOutCaps)));
                    if (err != 0)
                        throw new Win32Exception (err);
					yield return new WinMMPortDetails (i, caps.Name, caps.DriverVersion);
				}
			}
		}

		public event EventHandler<MidiConnectionEventArgs> StateChanged;

		public Task<IMidiInput> OpenInputAsync(string portId)
		{
			var details = Inputs.FirstOrDefault(d => d.Id == portId);
			if (details == null)
				throw new InvalidOperationException($"The device with ID {portId} is not found.");
			return Task.FromResult((IMidiInput)new WinMMMidiInput(details));
		}

		public Task<IMidiOutput> OpenOutputAsync(string portId)
		{
			var details = Outputs.FirstOrDefault(d => d.Id == portId);
			if (details == null)
				throw new InvalidOperationException($"The device with ID {portId} is not found.");
			return Task.FromResult((IMidiOutput)new WinMMMidiOutput(details));
		}
	}

	class WinMMPortDetails : IMidiPortDetails
	{
		public WinMMPortDetails(uint deviceId, string name, int version)
		{
			Id = deviceId.ToString();
			Name = name;
			Version = version.ToString();
		}

		public string Id { get; private set; }

		public string Manufacturer { get; private set; }

		public string Name { get; private set; }

		public string Version { get; private set; }
	}

	class WinMMMidiInput : IMidiInput
	{
		public WinMMMidiInput(IMidiPortDetails details)
		{
			Details = details;
			WinMMNatives.midiInOpen(out handle, uint.Parse(Details.Id), HandleMidiInProc, IntPtr.Zero, MidiInOpenFlags.Function);
			Connection = MidiPortConnectionState.Open;
		}

		IntPtr handle;
		byte[] data2b = new byte[2];
		byte[] data3b = new byte[3];

		// How does it dispatch SYSEX mesasges...
		void HandleMidiInProc(IntPtr midiIn, uint msg, ref int instance, ref int param1, ref int param2)
		{
			if (MessageReceived != null)
			{
				var status = (byte)(param1 & 0xFF);
				var msb = (byte)((param1 & 0xFF00) >> 8);
				var lsb = (byte)((param1 & 0xFF0000) >> 16);
				var data = MidiEvent.FixedDataSize(status) == 3 ? data3b : data2b;
				data[0] = status;
				data[1] = msb;
				if (data.Length == 3)
					data[2] = lsb;
				MessageReceived(this, new MidiReceivedEventArgs() { Data = data, Start = 0, Length = data.Length, Timestamp = 0 });
			}
		}

		public IMidiPortDetails Details { get; private set; }

		public MidiPortConnectionState Connection { get; private set; }

		public event EventHandler<MidiReceivedEventArgs> MessageReceived;

		public Task CloseAsync()
		{
			return Task.Run(() =>
			{
				Connection = MidiPortConnectionState.Pending;
				WinMMNatives.midiInClose(handle);
				Connection = MidiPortConnectionState.Closed;
			});
		}

		public void Dispose()
		{
			CloseAsync().RunSynchronously();
		}
	}

	class WinMMMidiOutput : IMidiOutput
	{
		public WinMMMidiOutput (IMidiPortDetails details)
		{
			Details = details;
			WinMMNatives.midiOutOpen (out handle, uint.Parse (Details.Id), null, IntPtr.Zero, MidiOutOpenFlags.Null);
			Connection = MidiPortConnectionState.Open;
		}

		IntPtr handle;

		public IMidiPortDetails Details { get; private set; }

		public MidiPortConnectionState Connection { get; private set; }

		public Task CloseAsync()
		{
			return Task.Run(() =>
			{
				Connection = MidiPortConnectionState.Pending;
				WinMMNatives.midiOutClose(handle);
				Connection = MidiPortConnectionState.Closed;
			});
		}

		public void Dispose ()
		{
			CloseAsync ().RunSynchronously ();
		}

		public void Send (byte [] mevent, int offset, int length, long timestamp)
		{
			foreach (var evt in MidiEvent.Convert (mevent, offset, length)) {
				if (evt.StatusByte < 0xF0)
					WinMMNatives.midiOutShortMsg (handle, (uint)(evt.StatusByte + (evt.Msb << 8) + (evt.Lsb << 16)));
				else {
					MidiHdr sysex = default (MidiHdr);
					unsafe {
						fixed (void* ptr = evt.Data) {
							sysex.Data = (IntPtr)ptr;
							sysex.BufferLength = evt.Data.Length;
							sysex.Flags = 0;
							WinMMNatives.midiOutPrepareHeader (handle, ref sysex, (uint)Marshal.SizeOf (typeof (MidiHdr)));
							WinMMNatives.midiOutLongMsg (handle, ref sysex, evt.Data.Length);
						}
					}
				}
			}
		}
	}
}
