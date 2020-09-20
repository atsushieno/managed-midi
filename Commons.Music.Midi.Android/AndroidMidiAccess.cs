using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Media.Midi;
using Android.Runtime;
using Commons.Music.Midi.Driod;
using Org.Billthefarmer.Mididriver;

namespace Commons.Music.Midi.Droid
{
	public class MidiSystem : MidiAccessManager
	{
		public static void Initialize (Context context)
		{
			Default = new AndroidMidiAccess (context);
		}
	}

	public class AndroidMidiAccess : IMidiAccess, IDisposable
	{
		Intent _midiServiceIntent;
		MidiManager _midiManager;

		ObservableCollection<IMidiPortDetails> _inputs = new ObservableCollection<IMidiPortDetails>();
		ObservableCollection<IMidiPortDetails> _outputs = new ObservableCollection<IMidiPortDetails>();
		public ObservableCollection<IMidiPortDetails> Inputs
		{
			get { return _inputs;  }
		}

		public ObservableCollection<IMidiPortDetails> Outputs
		{
			get { return _outputs; }
		}
		List<MidiDeviceInfo> _devices = null;
		List<MidiDevice> open_devices = new List<MidiDevice>();
		private bool _disposedValue;
        private MidiDeviceCallback _midiCallback;
		
        public AndroidMidiAccess (Context context)
		{
			_midiServiceIntent = new Intent(context, typeof(MidiSynthDeviceService));
			//_midiServiceIntent.SetAction("android.media.midi.MidiDeviceService");
			context.StartService(_midiServiceIntent);

			_midiManager = context.GetSystemService (Context.MidiService).JavaCast<MidiManager> ();
			
			_midiCallback = new MidiDeviceCallback();
            _midiCallback.DeviceAdded += OnMidiDeviceAdded;
            _midiCallback.DeviceRemoved += OnMidiDeviceRemoved;
            _midiCallback.DeviceStatusChanged += OnMidiDeviceStatusChanged;

			UpdateInputDevices(false);
			UpdateOutputDevices(false);
		}

        ~AndroidMidiAccess()
        {
			Dispose(false);
        }

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_midiManager.UnregisterDeviceCallback(_midiCallback);
				}
				_midiCallback.DeviceAdded -= OnMidiDeviceAdded;
				_midiCallback.DeviceRemoved -= OnMidiDeviceRemoved;
				_midiCallback.DeviceStatusChanged -= OnMidiDeviceStatusChanged;

				_disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		private void OnMidiDeviceStatusChanged(object sender, MidiDeviceStatus e)
		{
		}

		private void OnMidiDeviceRemoved(object sender, MidiDeviceInfo e)
		{
			_devices = null;
			UpdateInputDevices(true);
			UpdateOutputDevices(true);
		}

		private void OnMidiDeviceAdded(object sender, MidiDeviceInfo e)
		{
			_devices = null;
			UpdateInputDevices(false);
			UpdateOutputDevices(false);
		}

		private void UpdateInputDevices(bool hasRemovedDevice)
		{
			List<IMidiPortDetails> deviceList = GetInputDevices();
			if (hasRemovedDevice)
			{
				IMidiPortDetails[] array = _inputs.ToArray();
				foreach (var device in array)
				{
					if (!deviceList.Where(d => d.Id.Equals(device.Id)).Any())
					{
						_inputs.Remove(device);
					}
				}
			}
			foreach (var device in deviceList)
			{
				if (!_inputs.Where(d => d.Id.Equals(device.Id)).Any())
				{
					_inputs.Add(device);
				}
			}
		}

        private void UpdateOutputDevices(bool hasRemovedDevice)
		{
			List<IMidiPortDetails> deviceList = GetOutputDevices();
			if (hasRemovedDevice)
			{
				IMidiPortDetails[] array = _outputs.ToArray();
				foreach (var device in array)
				{
					if (!deviceList.Where(d => d.Id.Equals(device.Id)).Any())
					{
						_outputs.Remove(device);
					}
				}
			}
			foreach (var device in deviceList)
			{
				if (!_outputs.Where(d => d.Id.Equals(device.Id)).Any())
				{
					_outputs.Add(device);
				}
			}
		}
		private List<IMidiPortDetails> GetInputDevices()
		{
			List<MidiDeviceInfo> devices = GetDevices();
			return new List<IMidiPortDetails>(devices.SelectMany(d => d.GetPorts().Where(p => p.Type == MidiPortType.Input).Select(p => new MidiPortDetails(d, p)))); 
		}

        private List<IMidiPortDetails> GetOutputDevices()
        {
			List<MidiDeviceInfo> devices = GetDevices();
			return new List<IMidiPortDetails>(devices.SelectMany(d => d.GetPorts().Where(p => p.Type == MidiPortType.Output).Select(p => new MidiPortDetails(d, p))));
        }

		private List<MidiDeviceInfo> GetDevices()
		{
			if (_devices == null || _devices.Count == 0)
			{
				_devices = new List<MidiDeviceInfo>(_midiManager.GetDevices());
			}
			return _devices;
		}

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
						parent._midiManager.OpenDevice (port_to_open.Device, this, null);
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
}
