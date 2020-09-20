using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace Commons.Music.Midi
{
	public interface IMidiAccess
	{
		ObservableCollection<IMidiPortDetails> Inputs { get; }
		ObservableCollection<IMidiPortDetails> Outputs { get; }

		Task<IMidiInput> OpenInputAsync(string portId);
		Task<IMidiOutput> OpenOutputAsync(string portId);
	}

	
}
