using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreMidi;

namespace Commons.Music.Midi.iOS
{
	public class CoreMidiAccess : IMidiAccess
	{
		private MidiClient _client;
		private CoreMidiSynthesizer _midiSynthesizer;
		public ObservableCollection<IMidiPortDetails> Inputs { get; }

		public ObservableCollection<IMidiPortDetails> Outputs { get; }

		public CoreMidiAccess()
        {
			_midiSynthesizer = new CoreMidiSynthesizer();
			Inputs = new ObservableCollection<IMidiPortDetails>(GetInputDevices());

			Outputs = new ObservableCollection<IMidiPortDetails>(GetOutputDevices());
			Outputs.Add(_midiSynthesizer);

			_client = new MidiClient("CoreMidiSample MIDI CLient");
			_client.ObjectAdded += delegate (object sender, ObjectAddedOrRemovedEventArgs e) {
				UpdateInputDevices(false);
				UpdateOutputDevices(false);
			};
			_client.ObjectRemoved += delegate (object sender, ObjectAddedOrRemovedEventArgs e) {
				UpdateInputDevices(true);
				UpdateOutputDevices(true);
			};			
		}

		private void UpdateInputDevices(bool hasRemovedDevice)
		{
			List<IMidiPortDetails> deviceList = new List<IMidiPortDetails>(GetInputDevices());
			if (hasRemovedDevice)
			{
				IMidiPortDetails[] array = Inputs.ToArray();
				foreach (var device in array)
				{
					if (!deviceList.Where(d => d.Id.Equals(device.Id)).Any())
					{
						Inputs.Remove(device);
					}
				}
			}
			foreach (var device in deviceList)
			{
				if (!Inputs.Where(d => d.Id.Equals(device.Id)).Any())
				{
					Inputs.Add(device);
				}
			}
		}
		private void UpdateOutputDevices(bool hasRemovedDevice)
		{
			List<IMidiPortDetails> deviceList = new List<IMidiPortDetails>(GetOutputDevices());
			if (hasRemovedDevice)
			{
				IMidiPortDetails[] array = Outputs.ToArray();
				foreach (var device in array)
				{
					if (!deviceList.Where(d => d.Id.Equals(device.Id)).Any())
					{
						Outputs.Remove(device);
					}
				}
			}
			foreach (var device in deviceList)
			{
				if (!Outputs.Where(d => d.Id.Equals(device.Id)).Any())
				{
					Outputs.Add(device);
				}
			}
			if(!Outputs.Contains(_midiSynthesizer))
            {
				Outputs.Add(_midiSynthesizer);
            }
		}

		private IEnumerable<IMidiPortDetails> GetOutputDevices()
        {
            return Enumerable.Range(0, (int)CoreMidi.Midi.DestinationCount).Select(i => (IMidiPortDetails)new CoreMidiPortDetails(MidiEndpoint.GetDestination(i))); ;
		}

        private IEnumerable<IMidiPortDetails> GetInputDevices()
        {
           return Enumerable.Range(0, (int)CoreMidi.Midi.SourceCount).Select(i => (IMidiPortDetails)new CoreMidiPortDetails(MidiEndpoint.GetSource(i))); ;
		}

        private void UpdateDeviceLists()
        {
            ;
        }

        //public MidiAccessExtensionManager ExtensionManager { get; } = new CoreMidiAccessExtensionManager ();
		
        
        public event EventHandler<MidiConnectionEventArgs> StateChanged;

		public Task<IMidiInput> OpenInputAsync(string portId)
		{
			var details = Inputs.Cast<CoreMidiPortDetails> ().FirstOrDefault (i => i.Id == portId);
			if (details == null)
				throw new InvalidOperationException($"The device which is specified as port '{portId}' is not found.");
			return Task.FromResult((IMidiInput) new CoreMidiInput (details));
		}

		public Task<IMidiOutput> OpenOutputAsync(string portId)
		{
			var details = Outputs.Cast<IMidiPortDetails>().FirstOrDefault(i => i.Id == portId);
			if (details == null)
				throw new InvalidOperationException($"Device specified as port {portId} is not found.");
			if (details == _midiSynthesizer)
			{
				return Task.FromResult((IMidiOutput)_midiSynthesizer);
			}
			else
			{
				return Task.FromResult((IMidiOutput)new CoreMidiOutput(details));
			}
		}
	}

	

	

	
}
