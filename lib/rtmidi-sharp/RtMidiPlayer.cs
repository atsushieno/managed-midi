using System;
using Commons.Music.Midi;
using RtMidiSharp;
using Timer = System.Timers.Timer;

namespace Commons.Music.Midi.Player
{
	public class RtMidiPlayer : MidiPlayer
	{
		public RtMidiPlayer (RtMidiOutputDevice output, SmfMusic music)
			: base (music)
		{
			this.output = output;
			EventReceived += delegate (SmfEvent e) { SendMidiEvent (e); };
		}

		// it should not be disposed here. The module that
		// created this object should dispose it instead.
		RtMidiOutputDevice output;
		
		byte [] buf = new byte [3];

		void SendMidiEvent (SmfEvent m)
		{
			if ((m.Value & 0xFF) == 0xF0)
				WriteSysEx (0xF0, m.Data);
			else if ((m.Value & 0xFF) == 0xF7)
				WriteSysEx (0xF7, m.Data);
			else if ((m.Value & 0xFF) == 0xFF)
				return; // meta. Nothing to send.
			else {
				buf [0] = m.StatusByte;
				buf [1] = m.Msb;
				buf [2] = m.Lsb;
				output.SendMessage (buf);
			}
		}

		void WriteSysEx (byte status, byte [] sysex)
		{
			var buf = new byte [sysex.Length + 1];
			buf [0] = status;
			Array.Copy (sysex, 0, buf, 1, buf.Length - 1);
			output.SendMessage (buf);
		}
	}
}
