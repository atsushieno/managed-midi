using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Commons.Music.Midi
{
	public class MidiAccessManager
	{
		protected MidiAccessManager ()
		{
			Default = Empty = new EmptyMidiAccess();
		}

		public static IMidiAccess Default { get; protected set; }
		public static IMidiAccess Empty { get; internal set; }

	}
	
	

	
	

	

	

	
}
