using System;

namespace Commons.Music.Midi.RtMidi
{
	[Obsolete ("Now MidiPlayer class takes IMidiAccess so that you don't have to depend on implementation-specific player anymore.")]
	public class RtMidiPlayer : MidiPlayer
	{
		public RtMidiPlayer (RtMidiOutputDevice output, MidiMusic music)
			: base (music)
		{
			this.output = output;
			EventReceived += delegate (MidiEvent e) { SendMidiEvent (e); };
		}

		// it should not be disposed here. The module that
		// created this object should dispose it instead.
		RtMidiOutputDevice output;
		
		byte [] buf2 = new byte [2];
		byte [] buf3 = new byte [3];

		void SendMidiEvent (MidiEvent m)
		{
			if ((m.Value & 0xFF) == 0xF0)
				WriteSysEx (0xF0, m.Data);
			else if ((m.Value & 0xFF) == 0xF7)
				WriteSysEx (0xF7, m.Data);
			else if ((m.Value & 0xFF) == 0xFF)
				return; // meta. Nothing to send.
			else {
				switch (m.StatusByte & 0xF0) {
				case MidiEvent.Program:
				case MidiEvent.CAf:
					buf2 [0] = m.StatusByte;
					buf2 [1] = m.Msb;
					output.SendMessage (buf2, buf2.Length);
					break;
				default:
					buf3 [0] = m.StatusByte;
					buf3 [1] = m.Msb;
					buf3 [2] = m.Lsb;
					output.SendMessage (buf3, buf3.Length);
					break;
				}
			}
		}

		void WriteSysEx (byte status, byte [] sysex)
		{
			var buf = new byte [sysex.Length + 1];
			buf [0] = status;
			Array.Copy (sysex, 0, buf, 1, buf.Length - 1);
			output.SendMessage (buf, buf.Length);
		}
	}
}
