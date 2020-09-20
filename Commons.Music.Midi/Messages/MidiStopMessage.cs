namespace Commons.Music.Midi.Messages
{
    public class MidiStopMessage : MidiShortMessage
    {
        public MidiStopMessage(int deltaTime = 0)
                : base(MidiEvent.MidiStop, deltaTime)
        {
        }
    }
}