using System;
using System.Collections.Generic;

namespace Commons.Music.Midi
{
	public delegate void MidiEventAction (MidiEvent m);

	public class MidiMachine
	{
		public MidiMachine ()
		{
			var arr = new MidiMachineChannel [16];
			for (int i = 0; i < arr.Length; i++)
				arr [i] = new MidiMachineChannel ();
			Channels = arr;
		}

		public event MidiEventAction EventReceived;

		public IList<MidiMachineChannel> Channels { get; private set; }

		public virtual void ProcessEvent (MidiEvent evt)
		{
			switch (evt.EventType) {
			case MidiEvent.NoteOn:
				Channels [evt.Channel].NoteVelocity [evt.Msb] = evt.Lsb;
				break;
			case MidiEvent.NoteOff:
				Channels [evt.Channel].NoteVelocity [evt.Msb] = 0;
				break;
			case MidiEvent.PAf:
				Channels [evt.Channel].PAfVelocity [evt.Msb] = evt.Lsb;
				break;
			case MidiEvent.CC:
				// FIXME: handle RPNs and NRPNs by DTE
				switch (evt.Msb) {
				case MidiCC.NrpnMsb:
				case MidiCC.NrpnLsb:
					Channels [evt.Channel].DteTarget = DteTarget.Nrpn;
					break;
				case MidiCC.RpnMsb:
				case MidiCC.RpnLsb:
					Channels [evt.Channel].DteTarget = DteTarget.Rpn;
					break;
				case MidiCC.DteMsb:
					Channels [evt.Channel].ProcessDte (evt.Lsb, true);
					break;
				case MidiCC.DteLsb:
					Channels [evt.Channel].ProcessDte (evt.Lsb, false);
					break;
				case MidiCC.DteIncrement:
					Channels [evt.Channel].ProcessDteIncrement ();
					break;
				case MidiCC.DteDecrement:
					Channels [evt.Channel].ProcessDteDecrement ();
					break;
				}
				Channels [evt.Channel].Controls [evt.Msb] = evt.Lsb;
				break;
			case MidiEvent.Program:
				Channels [evt.Channel].Program = evt.Msb;
				break;
			case MidiEvent.CAf:
				Channels [evt.Channel].CAf = evt.Msb;
				break;
			case MidiEvent.Pitch:
				Channels [evt.Channel].PitchBend = (short) ((evt.Msb << 7) + evt.Lsb);
				break;
			}
			if (EventReceived != null)
				EventReceived (evt);
		}
	}
	
	public class MidiMachineChannel
	{
		public byte [] NoteVelocity = new byte [128];
		public byte [] PAfVelocity = new byte [128];
		public byte [] Controls = new byte [128];
		public short [] RPNs = new short [128]; // only 5 should be used though
		public short [] NRPNs = new short [128];
		public byte Program { get; set; }
		public byte CAf { get; set; }
		public short PitchBend { get; set; }
		public DteTarget DteTarget { get; set; }

		byte dte_target;

		public short RpnTarget {
			get { return (short) ((Controls [MidiCC.RpnMsb] << 7) + Controls [MidiCC.RpnLsb]); }
		}

		public void ProcessDte (byte value, bool msb)
		{
			short [] arr;
			switch (DteTarget) {
			case DteTarget.Rpn:
				dte_target = Controls [msb ? MidiCC.RpnMsb : MidiCC.RpnLsb];
				arr = RPNs;
				break;
			case DteTarget.Nrpn:
				dte_target = Controls [msb ? MidiCC.NrpnMsb : MidiCC.NrpnLsb];
				arr = NRPNs;
				break;
			default:
				return;
			}
			short cur = arr [dte_target];
			if (msb)
				arr [dte_target] = (short) (cur & 0x007F + ((value & 0x7F) << 7));
			else
				arr [dte_target] = (short) (cur & 0x3FF0 + (value & 0x7F));
		}

		public void ProcessDteIncrement ()
		{
			switch (DteTarget) {
			case DteTarget.Rpn:
				RPNs [dte_target]++;
				break;
			case DteTarget.Nrpn:
				NRPNs [dte_target]++;
				break;
			}
		}

		public void ProcessDteDecrement ()
		{
			switch (DteTarget) {
			case DteTarget.Rpn:
				RPNs [dte_target]--;
				break;
			case DteTarget.Nrpn:
				NRPNs [dte_target]--;
				break;
			}
		}
	}
		
	public enum DteTarget
	{
		Rpn,
		Nrpn
	}
}
