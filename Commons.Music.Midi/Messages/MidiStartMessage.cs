namespace Commons.Music.Midi.Messages
{
    public class MidiStartMessage : MidiShortMessage
    {
        public MidiStartMessage(int deltaTime = 0)
                  : base(MidiEvent.MidiStart, deltaTime)
        {
        }
    }
}