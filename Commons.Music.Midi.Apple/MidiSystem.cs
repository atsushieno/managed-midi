using CoreMidi;
using Foundation;

#if __MOBILE__
namespace Commons.Music.Midi.iOS
#else
namespace Commons.Music.Midi.macOS
#endif
{
    public class MidiSystem : Commons.Music.Midi.MidiAccessManager
	{
		static NSUrl _pathToSoundFont;
		public static void Initialize(NSUrl pathToSoundFont)
		{
			_pathToSoundFont = pathToSoundFont;
#if __MOBILE__
			MidiNetworkSession session = MidiNetworkSession.DefaultSession;
			session.Enabled = true;
			session.ConnectionPolicy = MidiNetworkConnectionPolicy.Anyone;
#endif
			Default = new CoreMidiAccess();
		}

		public static NSUrl PathToSoundFont { get { return _pathToSoundFont; } }
	}

}
