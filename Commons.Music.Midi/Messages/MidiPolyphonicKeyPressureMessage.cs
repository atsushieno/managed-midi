namespace Commons.Music.Midi.Messages
{
    public class MidiPolyphonicKeyPressureMessage : MidiMessage
    {
        public MidiPolyphonicKeyPressureMessage(byte channel, byte note, byte pressure, int deltaTime = 0)
             : base(deltaTime, new MidiEvent((byte)(MidiEvent.PAf + (channel & 0x0f)), note, pressure))
        {
        }
    }
}