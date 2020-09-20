namespace Commons.Music.Midi.Messages
{
    public class MidiTimingClockMessage : MidiShortMessage
    {
        public MidiTimingClockMessage(int deltaTime = 0)
             : base(MidiEvent.MtcQuarterFrame, deltaTime)
        {
        }
    }
}