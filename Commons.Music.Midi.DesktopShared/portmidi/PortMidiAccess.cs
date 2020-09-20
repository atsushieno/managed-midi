using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Commons.Music.Midi.PortMidi
{
    public class PortMidiAccess : IMidiAccess
	{
		//Should be changed to something observable, by implementing a device watcher similar to the uwp one.
		public ObservableCollection<IMidiPortDetails> Inputs {
			get { return new ObservableCollection<IMidiPortDetails>(PortMidiDeviceManager.AllDevices.Where (d => d.IsInput).Select (d => new PortMidiPortDetails (d))); }
		}

		public ObservableCollection<IMidiPortDetails> Outputs {
			get { return new ObservableCollection<IMidiPortDetails>(PortMidiDeviceManager.AllDevices.Where (d => d.IsOutput).Select (d => new PortMidiPortDetails (d))); }
		}

		public event EventHandler<MidiConnectionEventArgs> StateChanged;

		public PortMidiAccess()
		{
			// This is dummy. It is just to try p/invoking portmidi.
			if (PortMidiDeviceManager.DeviceCount < 0)
				throw new InvalidOperationException ("unexpected negative device count.");
		}
		
		public Task<IMidiInput> OpenInputAsync (string portId)
		{
			var p = new PortMidiInput ((PortMidiPortDetails) Inputs.First (i => i.Id == portId));
			return p.OpenAsync ().ContinueWith (t => (IMidiInput) p);
		}
		
		public Task<IMidiOutput> OpenOutputAsync (string portId)
		{
			var p = new PortMidiOutput ((PortMidiPortDetails) Outputs.First (i => i.Id == portId));
			return p.OpenAsync ().ContinueWith (t => (IMidiOutput) p);
		}
	}

	class PortMidiPortDetails : IMidiPortDetails
	{
		public PortMidiPortDetails (PortMidiDeviceInfo deviceInfo)
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

	abstract class PortMidiPort : IMidiPort
	{
		static internal Task completed_task = Task.FromResult (false);

		protected PortMidiPort (PortMidiPortDetails portDetails)
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

	class PortMidiInput : PortMidiPort, IMidiInput
	{
		public PortMidiInput (PortMidiPortDetails portDetails)
			: base (portDetails)
		{
		}

		public event EventHandler<MidiReceivedEventArgs> MessageReceived;

		PortMidiInputStream impl;

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
			impl = PortMidiDeviceManager.OpenInput (((PortMidiPortDetails) Details).RawId);
			Connection = MidiPortConnectionState.Open;
			return completed_task;
		}
	}

	class PortMidiOutput : PortMidiPort, IMidiOutput
	{
		public PortMidiOutput (PortMidiPortDetails portDetails)
			: base (portDetails)
		{
		}

		PortMidiOutputStream impl;

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
			impl = PortMidiDeviceManager.OpenOutput (((PortMidiPortDetails) Details).RawId);
			Connection = MidiPortConnectionState.Open;
			return completed_task;
		}

		public void Send (byte [] mevent, int offset, int length, long timestamp)
		{
			if (mevent == null)
				throw new ArgumentNullException ("mevent");
			if (mevent.Length == 0)
				return; // do nothing
			var events = PortMidiStream.Convert (mevent, 0, length);
			if (events.Any ()) {
				var first = events.First ();
				first.Timestamp = (int) timestamp;
				impl.Write (first);
				foreach (var evt in events.Skip (1))
					impl.Write (evt);
			}
		}
	}
}
