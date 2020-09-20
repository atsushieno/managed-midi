namespace Commons.Music.Midi.Messages
{
    public class MidiProgramChangeMessage : MidiMessage
    {
        public MidiProgramChangeMessage(byte channel, byte program, int deltaTime = 0)
            : base(deltaTime, new MidiEvent((byte)(MidiEvent.Program+ (channel & 0x0f)), program,0))
        {
        }
    }
}