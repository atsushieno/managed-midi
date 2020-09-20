using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public interface IMidiPortDetails
	{
		string Id { get; }
		string Manufacturer { get; }
		string Name { get; }
		string Version { get; }
	}

}
