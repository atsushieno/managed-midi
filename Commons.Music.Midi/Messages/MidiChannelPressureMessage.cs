namespace Commons.Music.Midi.Messages
{
    public class MidiChannelPressureMessage : MidiMessage
    {
        public MidiChannelPressureMessage(byte channel, byte pressure, int deltatime=0)
            :base(deltatime, new MidiEvent((byte)(MidiEvent.CAf + (channel & 0x0f)),pressure,0))
        {
        }
    }
}