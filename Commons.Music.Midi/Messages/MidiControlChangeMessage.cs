namespace Commons.Music.Midi.Messages
{
    public class MidiControlChangeMessage : MidiMessage
    {
        public MidiControlChangeMessage(byte channel, byte controller, byte controlValue, int deltaTime = 0)
            :base(deltaTime,new MidiEvent((byte)(MidiEvent.CC + (channel & 0x0f)),controller,controlValue))
        {
        }
    }
}