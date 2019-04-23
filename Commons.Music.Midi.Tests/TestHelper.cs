namespace Commons.Music.Midi.Tests
{
	public class TestHelper
	{
		public static MidiMusic GetMidiMusic ()
		{
			var music = new MidiMusic ();
			music.DeltaTimeSpec = 192;
			var track = new MidiTrack ();
			byte ch = 1;
			track.Messages.Add (new MidiMessage (188, new MidiEvent ((byte) (MidiEvent.Program + ch), 1, 0, null, 0, 0)));
			for (int i = 0; i < 100; i++) {
				track.Messages.Add (
					new MidiMessage (4, new MidiEvent ((byte) (MidiEvent.NoteOn + ch), 60, 120, null, 0, 0)));
				track.Messages.Add (
					new MidiMessage (44, new MidiEvent ((byte) (MidiEvent.NoteOff + ch), 60, 0, null, 0, 0)));
			}

			music.Tracks.Add (track);
			return music;
		}
		
		public static MidiMusic GetMidiMusic (string resourceId)
		{
			using (var stream = typeof (TestHelper).Assembly.GetManifestResourceStream (resourceId))
				return MidiMusic.Read (stream);
		}

		public static MidiPlayer GetMidiPlayer (IMidiPlayerTimeManager timeManager, MidiMusic midiMusic, IMidiAccess midiAccess = null)
		{
			var access = midiAccess ?? MidiAccessManager.Empty;
			var music = midiMusic ?? GetMidiMusic ();
			var tm = timeManager ?? new VirtualMidiPlayerTimeManager ();
			return new MidiPlayer (music, access, tm);
		}

		public static MidiPlayer GetMidiPlayer (IMidiPlayerTimeManager timeManager = null, IMidiAccess midiAccess = null, string resourceId = null)
		{
			var access = midiAccess ?? MidiAccessManager.Empty;
			var music = resourceId != null ? GetMidiMusic (resourceId) : GetMidiMusic ();
			var tm = timeManager ?? new VirtualMidiPlayerTimeManager ();
			return new MidiPlayer (music, access, tm);
		}		
	}
}