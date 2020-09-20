namespace Commons.Music.Midi.Messages
{
    public class MidiNoteOnMessage : MidiMessage
    {
        public MidiNoteOnMessage(byte channel, byte note, byte velocity, int deltaTime = 0)
            : base(deltaTime, new MidiEvent((byte)(MidiEvent.NoteOn + (channel & 0x0f)), note, velocity))
        {
        }
    }
}