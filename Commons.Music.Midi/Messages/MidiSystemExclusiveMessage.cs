namespace Commons.Music.Midi.Messages
{
    public class MidiSystemExclusiveMessage : MidiMessage
    {
        public MidiSystemExclusiveMessage(byte[] rawData, int deltaTime = 0)
            : base(deltaTime, new MidiEvent(MidiEvent.SysEx1, 0, 0, rawData))
        {
        }
    }
}