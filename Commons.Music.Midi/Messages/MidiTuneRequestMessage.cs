namespace Commons.Music.Midi.Messages
{
    public class MidiTuneRequestMessage :MidiShortMessage
    {
        public MidiTuneRequestMessage(int deltaTime = 0)
            : base(MidiEvent.TuneRequest, deltaTime)
        {
        }
    }
}