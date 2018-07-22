using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Commons.Music.Midi.RtMidi
{
	public class RtMidiAccess : IMidiAccess
	{
		public IEnumerable<IMidiPortDetails> Inputs {
			get { return MidiDeviceManager.AllDevices.Where (d => d.IsInput).Select (d => new RtMidiPortDetails (d)); }
		}

		public IEnumerable<IMidiPortDetails> Outputs {
			get { return MidiDeviceManager.AllDevices.Where (d => d.IsOutput).Select (d => new RtMidiPortDetails (d)); }
		}
		
		public Task<IMidiInput> OpenInputAsync (string portId)
		{
			var p = new RtMidiInput ((RtMidiPortDetails) Inputs.First (i => i.Id == portId));
			return p.OpenAsync ().ContinueWith (t => (IMidiInput) p);
		}
		
		public Task<IMidiOutput> OpenOutputAsync (string portId)
		{
			var p = new RtMidiOutput ((RtMidiPortDetails) Outputs.First (i => i.Id == portId));
			return p.OpenAsync ().ContinueWith (t => (IMidiOutput) p);
		}

		public event EventHandler<MidiConnectionEventArgs> StateChanged;
	}

	class RtMidiPortDetails : IMidiPortDetails
	{
		public RtMidiPortDetails (MidiDeviceInfo deviceInfo)
		{
			RawId = deviceInfo.ID;
			Id = deviceInfo.ID.ToString ();
			// okay, it is not really manufacturer
			Manufacturer = deviceInfo.Interface;
			Name = deviceInfo.Name;
			Version = string.Empty;
		}

		public int RawId { get; private set; }

		public string Id { get; private set; }

		public string Manufacturer { get; private set; }

		public string Name { get; private set; }

		public string Version { get; private set; }
	}

	abstract class RtMidiPort : IMidiPort
	{
		static internal Task completed_task = Task.FromResult (false);

		protected RtMidiPort (RtMidiPortDetails portDetails)
		{
			if (portDetails == null)
				throw new ArgumentNullException ("portDetails");
			Details = portDetails;
			Connection = MidiPortConnectionState.Closed;
		}

		public MidiPortConnectionState Connection { get; internal set; }
		public IMidiPortDetails Details { get; private set; }

		public abstract Task CloseAsync ();
		public abstract Task OpenAsync ();

		public void Dispose ()
		{
			if (Connection == MidiPortConnectionState.Open)
				CloseAsync ();
		}
	}

	class RtMidiInput : RtMidiPort, IMidiInput
	{
		public RtMidiInput (RtMidiPortDetails portDetails)
			: base (portDetails)
		{
		}

		public event EventHandler<MidiReceivedEventArgs> MessageReceived;

		RtMidiInputDevice impl;

		public override Task CloseAsync ()
		{
			if (Connection != MidiPortConnectionState.Open || impl == null)
				throw new InvalidOperationException ("No open input.");
			impl.Close ();
			Connection = MidiPortConnectionState.Closed;
			return completed_task;
		}

		public unsafe override Task OpenAsync ()
		{
			Connection = MidiPortConnectionState.Pending;
			impl = MidiDeviceManager.OpenInput (((RtMidiPortDetails)Details).RawId);
			impl.SetCallback ((timestamp, message, size, userData) => {
				var bytes = new byte [size];
				System.Runtime.InteropServices.Marshal.Copy ((IntPtr) message, bytes, 0, (int) size);
				MessageReceived (this, new MidiReceivedEventArgs { Data = bytes, Start = 0, Length = bytes.Length, Timestamp = (long) timestamp });
			}, IntPtr.Zero);
			Connection = MidiPortConnectionState.Open;
			return completed_task;
		}
	}

	class RtMidiOutput : RtMidiPort, IMidiOutput
	{
		public RtMidiOutput (RtMidiPortDetails portDetails)
			: base (portDetails)
		{
		}

		RtMidiOutputDevice impl;

		public override Task CloseAsync ()
		{
			if (Connection != MidiPortConnectionState.Open || impl == null)
				throw new InvalidOperationException ("No open output.");
			impl.Close ();
			Connection = MidiPortConnectionState.Closed;
			return completed_task;
		}

		public override Task OpenAsync ()
		{
			Connection = MidiPortConnectionState.Pending;
			impl = MidiDeviceManager.OpenOutput (((RtMidiPortDetails) Details).RawId);
			Connection = MidiPortConnectionState.Open;
			return completed_task;
		}

		public void Send (byte [] mevent, int offset, int length, long timestamp)
		{
			if (timestamp > 0)
				throw new InvalidOperationException ("non-zero timestamp is not supported");
			if (mevent == null)
				throw new ArgumentNullException ("mevent");
			if (mevent.Length == 0)
				return; // do nothing
			impl.SendMessage (mevent, length);
		}
	}
}
