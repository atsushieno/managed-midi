using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace Commons.Music.Midi
{
	public class EmptyMidiAccess : IMidiAccess
	{
		static ObservableCollection<IMidiPortDetails> collection = new ObservableCollection<IMidiPortDetails>();
		public ObservableCollection<IMidiPortDetails> Inputs
		{
			get { return collection; }
		}

		public ObservableCollection<IMidiPortDetails> Outputs
		{
			get { return collection; }
		}

		public Task<IMidiInput> OpenInputAsync(string portId)
		{
			if (portId != EmptyMidiInput.Instance.Details.Id)
				throw new ArgumentException(string.Format("Port ID {0} does not exist.", portId));
			return Task.FromResult<IMidiInput>(EmptyMidiInput.Instance);
		}

		public Task<IMidiOutput> OpenOutputAsync(string portId)
		{
			if (portId != EmptyMidiOutput.Instance.Details.Id)
				throw new ArgumentException(string.Format("Port ID {0} does not exist.", portId));
			return Task.FromResult<IMidiOutput>(EmptyMidiOutput.Instance);
		}

#pragma warning disable 0067
		// it will never be fired.
		public event EventHandler<MidiConnectionEventArgs> StateChanged;
#pragma warning restore 0067
	}

	

	

	
}
