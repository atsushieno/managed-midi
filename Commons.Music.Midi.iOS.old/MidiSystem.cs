using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreMidi;
using Foundation;
using UIKit;

namespace Commons.Music.Midi.iOS
{
	public class MidiSystem : Commons.Music.Midi.MidiAccessManager
	{
		static NSUrl _pathToSoundFont;
		public static void Initialize(NSUrl pathToSoundFont)
		{
			_pathToSoundFont = pathToSoundFont;
			MidiNetworkSession session = MidiNetworkSession.DefaultSession;
			session.Enabled = true;
			session.ConnectionPolicy = MidiNetworkConnectionPolicy.Anyone;

			Default = new CoreMidiAccess();
		}

		public static NSUrl PathToSoundFont { get { return _pathToSoundFont; } }
	}

}
