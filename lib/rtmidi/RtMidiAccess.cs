using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Commons.Music.Midi.RtMidi
{
	public class RtMidiAccess : IMidiAccess
	{
		public IEnumerable<IMidiInput> Inputs {
			get { return MidiDeviceManager.AllDevices.Where (d => d.IsInput).Select (d => new RtMidiInput (d)); }
		}

		public IEnumerable<IMidiOutput> Outputs {
			get { return MidiDeviceManager.AllDevices.Where (d => d.IsOutput).Select (d => new RtMidiOutput (d)); }
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

		protected RtMidiPort (MidiDeviceInfo deviceInfo)
		{
			this.info = deviceInfo;
			Details = new RtMidiPortDetails (info);
			Connection = MidiPortConnectionState.Closed;
			State = MidiPortDeviceState.Connected; // there is no way to check that...
		}

		MidiDeviceInfo info;

		public MidiPortConnectionState Connection { get; internal set; }
		public IMidiPortDetails Details { get; private set; }
		public MidiPortDeviceState State { get; internal set; }

		public event EventHandler StateChanged;

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
		public RtMidiInput (MidiDeviceInfo info)
			: base (info)
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

		public override Task OpenAsync ()
		{
			Connection = MidiPortConnectionState.Pending;
			impl = MidiDeviceManager.OpenInput (((RtMidiPortDetails) Details).RawId);
			Connection = MidiPortConnectionState.Open;
			return completed_task;
		}
	}

	class RtMidiOutput : RtMidiPort, IMidiOutput
	{
		public RtMidiOutput (MidiDeviceInfo info)
			: base (info)
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

		public Task SendAsync (byte [] mevent, int length, long timestamp)
		{
			if (timestamp > 0)
				throw new InvalidOperationException ("non-zero timestamp is not supported");
			if (mevent == null)
				throw new ArgumentNullException ("mevent");
			if (mevent.Length == 0)
				return completed_task; // do nothing
			impl.SendMessage (mevent, length);
			return completed_task;
		}
	}
}
