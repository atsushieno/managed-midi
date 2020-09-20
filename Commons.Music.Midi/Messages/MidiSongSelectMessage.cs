namespace Commons.Music.Midi.Messages
{
    public class MidiSongSelectMessage : MidiMessage
    {
        public MidiSongSelectMessage(byte song, int deltaTime = 0)
            : base(deltaTime, new MidiEvent(MidiEvent.SongSelect, 0, song))
        { 
        }
    }
}