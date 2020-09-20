namespace Commons.Music.Midi.Messages
{
    public class MidiNoteOffMessage: MidiMessage
    {
   
        public MidiNoteOffMessage(byte channel, byte note, byte velocity, int deltaTime=0)
            :base(deltaTime,new MidiEvent((byte)(MidiEvent.NoteOff + (channel & 0x0f)),note,velocity))
        {           
        }
    }
}