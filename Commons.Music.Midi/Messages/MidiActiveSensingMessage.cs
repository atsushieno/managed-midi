namespace Commons.Music.Midi.Messages
{
    public class MidiActiveSensingMessage : MidiShortMessage
    {
        public MidiActiveSensingMessage(int delta = 0)
            :base(MidiEvent.ActiveSense, delta)
        {
        }
    }
}