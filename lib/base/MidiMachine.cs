using System;
using System.Collections.Generic;

namespace Commons.Music.Midi
{
	public delegate void MidiMessageAction (SmfMessage m);

	public class MidiMachine
	{
		public MidiMachine ()
		{
			var arr = new MidiMachineChannel [16];
			for (int i = 0; i < arr.Length; i++)
				arr [i] = new MidiMachineChannel ();
			Channels = arr;
		}

		public event MidiMessageAction MessageReceived;

		public IList<MidiMachineChannel> Channels { get; private set; }

		public virtual void ProcessMessage (SmfMessage msg)
		{
			switch (msg.MessageType) {
			case SmfMessage.NoteOn:
				Channels [msg.Channel].NoteVelocity [msg.Msb] = msg.Lsb;
				break;
			case SmfMessage.NoteOff:
				Channels [msg.Channel].NoteVelocity [msg.Msb] = 0;
				break;
			case SmfMessage.PAf:
				Channels [msg.Channel].PAfVelocity [msg.Msb] = msg.Lsb;
				break;
			case SmfMessage.CC:
				// FIXME: handle RPNs and NRPNs by DTE
				switch (msg.Msb) {
				case SmfCC.NrpnMsb:
				case SmfCC.NrpnLsb:
					Channels [msg.Channel].DteTarget = DteTarget.Nrpn;
					break;
				case SmfCC.RpnMsb:
				case SmfCC.RpnLsb:
					Channels [msg.Channel].DteTarget = DteTarget.Rpn;
					break;
				case SmfCC.DteMsb:
					Channels [msg.Channel].ProcessDte (msg.Lsb, true);
					break;
				case SmfCC.DteLsb:
					Channels [msg.Channel].ProcessDte (msg.Lsb, false);
					break;
				case SmfCC.DteIncrement:
					Channels [msg.Channel].ProcessDteIncrement ();
					break;
				case SmfCC.DteDecrement:
					Channels [msg.Channel].ProcessDteDecrement ();
					break;
				}
				Channels [msg.Channel].Controls [msg.Msb] = msg.Lsb;
				break;
			case SmfMessage.Program:
				Channels [msg.Channel].Program = msg.Msb;
				break;
			case SmfMessage.CAf:
				Channels [msg.Channel].CAf = msg.Msb;
				break;
			case SmfMessage.Pitch:
				Channels [msg.Channel].PitchBend = (short) ((msg.Msb << 7) + msg.Lsb);
				break;
			}
			if (MessageReceived != null)
				MessageReceived (msg);
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
			get { return (short) ((Controls [SmfCC.RpnMsb] << 7) + Controls [SmfCC.RpnLsb]); }
		}

		public void ProcessDte (byte value, bool msb)
		{
			short [] arr;
			switch (DteTarget) {
			case DteTarget.Rpn:
				dte_target = Controls [msb ? SmfCC.RpnMsb : SmfCC.RpnLsb];
				arr = RPNs;
				break;
			case DteTarget.Nrpn:
				dte_target = Controls [msb ? SmfCC.NrpnMsb : SmfCC.NrpnLsb];
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
