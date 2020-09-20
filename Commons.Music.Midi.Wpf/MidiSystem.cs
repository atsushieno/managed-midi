using Commons.Music.Midi.WinMM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commons.Music.Midi.WinMM
{
	public class MidiSystem : Commons.Music.Midi.MidiAccessManager
	{
		public static void Initialize()
		{
			Default = new WinMMMidiAccess();
		}
	}

}
