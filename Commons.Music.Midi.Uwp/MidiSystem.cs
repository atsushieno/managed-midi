using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commons.Music.Midi.Uwp
{
	public class MidiSystem : Commons.Music.Midi.MidiAccessManager
	{
		public static void Initialize(Windows.UI.Core.CoreDispatcher dispatcher)
		{
			Default = new UwpMidiAccess(dispatcher);
		}
	}

}
