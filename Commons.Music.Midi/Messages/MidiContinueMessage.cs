namespace Commons.Music.Midi.Messages
{
    public class MidiContinueMessage : MidiShortMessage
    {
        public MidiContinueMessage(int deltaTime = 0)
            : base(MidiEvent.MidiContinue, deltaTime)
        {
        }
    }
}