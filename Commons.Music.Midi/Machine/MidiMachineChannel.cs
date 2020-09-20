using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public class MidiMachineChannel
	{
		public byte[] NoteVelocity = new byte[128];
		public byte[] PAfVelocity = new byte[128];
		public byte[] Controls = new byte[128];
		public short[] RPNs = new short[128]; // only 5 should be used though
		public short[] NRPNs = new short[128];
		public byte Program { get; set; }
		public byte CAf { get; set; }
		public short PitchBend { get; set; }
		public DteTarget DteTarget { get; set; }

		byte dte_target;

		public short RpnTarget
		{
			get { return (short)((Controls[MidiCC.RpnMsb] << 7) + Controls[MidiCC.RpnLsb]); }
		}

		public void ProcessDte(byte value, bool msb)
		{
			short[] arr;
			switch (DteTarget)
			{
				case DteTarget.Rpn:
					dte_target = Controls[msb ? MidiCC.RpnMsb : MidiCC.RpnLsb];
					arr = RPNs;
					break;
				case DteTarget.Nrpn:
					dte_target = Controls[msb ? MidiCC.NrpnMsb : MidiCC.NrpnLsb];
					arr = NRPNs;
					break;
				default:
					return;
			}
			short cur = arr[dte_target];
			if (msb)
				arr[dte_target] = (short)(cur & 0x007F + ((value & 0x7F) << 7));
			else
				arr[dte_target] = (short)(cur & 0x3FF0 + (value & 0x7F));
		}

		public void ProcessDteIncrement()
		{
			switch (DteTarget)
			{
				case DteTarget.Rpn:
					RPNs[dte_target]++;
					break;
				case DteTarget.Nrpn:
					NRPNs[dte_target]++;
					break;
			}
		}

		public void ProcessDteDecrement()
		{
			switch (DteTarget)
			{
				case DteTarget.Rpn:
					RPNs[dte_target]--;
					break;
				case DteTarget.Nrpn:
					NRPNs[dte_target]--;
					break;
			}
		}
	}
}
