namespace Commons.Music.Midi.Messages
{
    public class MidiPitchBendChangeMessage : MidiMessage
    {
        public MidiPitchBendChangeMessage(byte channel, ushort bend, int deltaTime=0)
            :base(deltaTime, new MidiEvent((byte)(MidiEvent.Pitch + (channel & 0x0f)), (byte)(bend & 0xff), (byte)(bend >>8)))
        {
        }
    }
}