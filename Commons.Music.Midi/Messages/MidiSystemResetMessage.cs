using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi.Messages
{
    public class MidiSystemResetMessage : MidiShortMessage
    {
        public MidiSystemResetMessage(int deltaTime = 0)
            : base(MidiEvent.Reset, deltaTime)
        {

        }
    }
}
