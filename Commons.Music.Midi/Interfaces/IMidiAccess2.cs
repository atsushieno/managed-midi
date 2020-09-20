using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public interface IMidiAccess2 : IMidiAccess
	{
		MidiAccessExtensionManager ExtensionManager { get; }
	}

}
