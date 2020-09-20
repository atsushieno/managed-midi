using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi.Messages
{
    public class MidiShortMessage : MidiMessage
    {
        public MidiShortMessage(byte messageType, int delta)
            : base(delta, new MidiEvent(messageType))
        {
        }
    }
}
