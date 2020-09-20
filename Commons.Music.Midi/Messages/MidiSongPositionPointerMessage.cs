namespace Commons.Music.Midi.Messages
{
    public class MidiSongPositionPointerMessage : MidiMessage
    {
        public MidiSongPositionPointerMessage(ushort beats, int deltaTime = 0)
            : base(deltaTime, new MidiEvent(MidiEvent.SongPositionPointer, 0, (byte)(beats & 0xff), (byte)(beats >> 8)))
        {
        }
    }
}