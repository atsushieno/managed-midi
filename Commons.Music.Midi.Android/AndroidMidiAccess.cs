using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Media.Midi;
using Android.Runtime;

namespace Commons.Music.Midi
{
	public partial class MidiAccessManager
	{
		partial void InitializeDefault ()
		{
			Default = new AndroidExtensions.AndroidMidiAccess (Android.App.Application.Context);
		}
	}
}

namespace Commons.Music.Midi.AndroidExtensions
{
	public class AndroidMidiAccess : IMidiAccess
	{
		MidiManager midi_manager;
		
		public AndroidMidiAccess (Context context)
		{
			midi_manager = context.GetSystemService (Context.MidiService).JavaCast<MidiManager> ();
		}
		
		public IEnumerable<IMidiPortDetails> Inputs {
			get { return midi_manager.GetDevices ().SelectMany (d => d.GetPorts ().Where (p => p.Type == MidiPortType.Input).Select (p => new MidiPortDetails (d, p))); }
		}

		public IEnumerable<IMidiPortDetails> Outputs {
			get { return midi_manager.GetDevices ().SelectMany (d => d.GetPorts ().Where (p => p.Type == MidiPortType.Output).Select (p => new MidiPortDetails (d, p))); }
		}

		// FIXME: left unsupported...
		public event EventHandler<MidiConnectionEventArgs> StateChanged;

		List<MidiDevice> open_devices = new List<MidiDevice> ();

		public Task<IMidiInput> OpenInputAsync (string portId)
		{
			var ip = (MidiPortDetails) Inputs.First (i => i.Id == portId);
			var dev = open_devices.FirstOrDefault (d => ip.Device.Id == d.Info.Id);
			var l = new OpenDeviceListener (this, dev, ip);
			return l.OpenInputAsync (CancellationToken.None);			
		}

		public Task<IMidiOutput> OpenOutputAsync (string portId)
		{
			var ip = (MidiPortDetails) Outputs.First (i => i.Id == portId);
			var dev = open_devices.FirstOrDefault (d => ip.Device.Id == d.Info.Id);
			var l = new OpenDeviceListener (this, dev, ip);
			return l.OpenOutputAsync (CancellationToken.None);			
		}

		class OpenDeviceListener : Java.Lang.Object, MidiManager.IOnDeviceOpenedListener
		{
			AndroidMidiAccess parent;
			MidiDevice device;
			MidiPortDetails port_to_open;
			ManualResetEventSlim wait;
			
			public OpenDeviceListener (AndroidMidiAccess parent, MidiDevice device, MidiPortDetails portToOpen)
			{
				if (parent == null)
					throw new ArgumentNullException (nameof (parent));
				if (portToOpen == null)
					throw new ArgumentNullException (nameof (portToOpen));
				this.parent = parent;
				this.device = device;
				port_to_open = portToOpen;
			}
			
			public Task<IMidiInput> OpenInputAsync (CancellationToken token)
			{
				// MidiInput takes Android.Media.Midi.MidiOutputPort because... Android.Media.Midi API sucks and MidiOutputPort represents a MIDI IN device(!!)
				return OpenAsync (token, dev => (IMidiInput) new MidiInput (port_to_open, dev.OpenOutputPort (port_to_open.Port.PortNumber)));
			}
			
			public Task<IMidiOutput> OpenOutputAsync (CancellationToken token)
			{
				// MidiOutput takes Android.Media.Midi.MidiInputPort because... Android.Media.Midi API sucks and MidiInputPort represents a MIDI OUT device(!!)
				return OpenAsync (token, dev => (IMidiOutput) new MidiOutput (port_to_open, dev.OpenInputPort (port_to_open.Port.PortNumber)));
			}
			
			Task<T> OpenAsync<T> (CancellationToken token, Func<MidiDevice, T> resultCreator)
			{
				return Task.Run (delegate {
					if (device == null) {
						wait = new ManualResetEventSlim ();
						parent.midi_manager.OpenDevice (port_to_open.Device, this, null);
						wait.Wait (token);
						wait.Reset ();
					}
					return resultCreator (device);
				});
			}
			
			public void OnDeviceOpened (MidiDevice device)
			{
				if (device == null)
					throw new ArgumentNullException (nameof (device));
				this.device = device;
				parent.open_devices.Add (device);
				wait.Set ();
			}
		}
	}

	public class MidiPortDetails : IMidiPortDetails
	{
		MidiDeviceInfo device;
		MidiDeviceInfo.PortInfo port;
		
		public MidiPortDetails (MidiDeviceInfo device, MidiDeviceInfo.PortInfo port)
		{
			if (device == null)
				throw new ArgumentNullException (nameof (device));
			if (port == null)
				throw new ArgumentNullException (nameof (port));
			this.device = device;
			this.port = port;
		}
		
		public MidiDeviceInfo Device {
			get { return device; }
		}
		
		public MidiDeviceInfo.PortInfo Port {
			get { return port; }
		}
		
		public string Id {
			get { return "device" + device.Id + "_port" + port.PortNumber; }
		}

		public string Manufacturer {
			get { return device.Properties.GetString (MidiDeviceInfo.PropertyManufacturer); }
		}

		public string Name {
			get { return port.Name; }
		}

		public string Version {
			get { return device.Properties.GetString (MidiDeviceInfo.PropertyVersion); }
		}
	}

	public class MidiPort : IMidiPort
	{
		MidiPortDetails details;
		MidiPortConnectionState connection;
		Action on_close;
		
		protected MidiPort (MidiPortDetails details, Action onClose)
		{
			this.details = details;
			on_close = onClose;
			connection = MidiPortConnectionState.Open;
		}
		
		public MidiPortConnectionState Connection {
			get { return connection; }
		}

		public IMidiPortDetails Details {
			get { return details; }
		}

		public Task CloseAsync ()
		{
			return Task.Run (() => { Close (); });
		}

		public void Dispose ()
		{
			Close ();
		}
		
		internal virtual void Close ()
		{
			on_close ();
			connection = MidiPortConnectionState.Closed;
		}
	}
	
	public class MidiInput : MidiPort, IMidiInput
	{
		MidiOutputPort port;
		Receiver receiver;
		
		public MidiInput (MidiPortDetails details, MidiOutputPort port)
			: base (details, () => port.Close ())
		{
			this.port = port;
			receiver = new Receiver (this);
			port.Connect (receiver);
		}
		
		internal override void Close ()
		{
			port.Disconnect (receiver);
			base.Close ();
		}
		
		class Receiver : MidiReceiver
		{
			MidiInput parent;
			
			public Receiver (MidiInput parent)
			{
				this.parent = parent;
			}
			
			public override void OnSend (byte [] msg, int offset, int count, long timestamp)
			{
				if (parent.MessageReceived != null)
					parent.MessageReceived (this, new MidiReceivedEventArgs () {
						Data = offset == 0 && msg.Length == count ? msg : msg.Skip (offset).Take (count).ToArray (),
						Timestamp = timestamp });
			}
		}

		public event EventHandler<MidiReceivedEventArgs> MessageReceived;
	}

	public class MidiOutput : MidiPort, IMidiOutput
	{
		MidiInputPort port;

		public MidiOutput (MidiPortDetails details, MidiInputPort port)
			: base (details, () => port.Close ())
		{
			this.port = port;
		}

		public Task SendAsync (byte [] mevent, int offset, int length, long timestamp)
		{
			// We could return Task.Run (), but it is stupid to create a Task instance for that on every call to this method.
			port.Send (mevent, offset, length, timestamp);
			return Task.FromResult (string.Empty);
		}
	}
}
