using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.ObjectModel;
using System.Threading;

namespace Commons.Music.Midi.Uwp
{
    public class UwpMidiAccess : IMidiAccess 
	{
		MidiDeviceWatcher _midiInDeviceWatcher;
		MidiDeviceWatcher _midiOutDeviceWatcher;

		public UwpMidiAccess (Windows.UI.Core.CoreDispatcher dispatcher)
		{
			_midiInDeviceWatcher = new MidiDeviceWatcher(MidiInPort.GetDeviceSelector(), dispatcher);
			_midiOutDeviceWatcher = new MidiDeviceWatcher(MidiOutPort.GetDeviceSelector(), dispatcher);
			_midiInDeviceWatcher.Start();
			_midiOutDeviceWatcher.Start();
		}

		public ObservableCollection<IMidiPortDetails> Inputs => _midiInDeviceWatcher.DeviceCollection;

		public ObservableCollection<IMidiPortDetails> Outputs => _midiOutDeviceWatcher.DeviceCollection;

		public event EventHandler<MidiConnectionEventArgs> StateChanged;

		public Task<IMidiInput> OpenInputAsync (string portId)
		{
			return Task<IMidiInput>.Run(async () =>
			{
				var inputs = Inputs;
				var details = inputs.Cast<UwpMidiPortDetails>().FirstOrDefault(d => d.Id.Equals(portId));
				var input = await MidiInPort.FromIdAsync(portId);
				return (IMidiInput) new UwpMidiInput(input, details);
			});
		}

		public async Task<IMidiOutput> OpenOutputAsync (string portId)
		{
			var outputs = Outputs;
			var details = outputs.Cast<UwpMidiPortDetails>().FirstOrDefault(d => d.Id.Equals(portId));
			var output = await MidiOutPort.FromIdAsync(details.Id);
			if (output != null)
			{
				return (IMidiOutput)new UwpMidiOutput(output, details);
			}
			return null;
		}
	}
}
