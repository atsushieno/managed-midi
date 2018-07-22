using System;

namespace Commons.Music.Midi.PortMidi
{
	[Obsolete ("Now MidiPlayer class takes IMidiAccess so that you don't have to depend on implementation-specific player anymore.")]
	public class PortMidiPlayer : MidiPlayer
	{
		public PortMidiPlayer (PortMidiOutputStream output, MidiMusic music)
			: base (music)
		{
			this.output = output;
			EventReceived += delegate (Midi.MidiEvent m) { SendMidiMessage(m); };
		}

		// it should not be disposed here. The module that
		// created this object should dispose it instead.
		PortMidiOutputStream output;

		void SendMidiMessage (Midi.MidiEvent m)
		{
			if ((m.Value & 0xFF) == 0xF0)
				WriteSysEx (0xF0, m.Data);
			else if ((m.Value & 0xFF) == 0xF7)
				WriteSysEx (0xF7, m.Data);
			else if ((m.Value & 0xFF) == 0xFF)
				return; // meta. Nothing to send.
			else
				output.Write (0, new PortMidiMessage (m.StatusByte, m.Msb, m.Lsb));
		}

		void WriteSysEx (byte status, byte [] sysex)
		{
			var buf = new byte [sysex.Length + 1];
			buf [0] = status;
			Array.Copy (sysex, 0, buf, 1, buf.Length - 1);
			output.WriteSysEx (0, buf);
		}
	}
}

