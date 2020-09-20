namespace Commons.Music.Midi.Messages
{
    public class MidiTimeCodeMessage : MidiMessage
    {
        public MidiTimeCodeMessage(byte frameType, byte values, int deltaTime = 0)
            : base(deltaTime, new MidiEvent(MidiEvent.MtcQuarterFrame, frameType, values))
        {
        }
    }
}