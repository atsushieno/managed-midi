using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public static class MidiCC
	{
		public const byte BankSelect = 0x00;
		public const byte Modulation = 0x01;
		public const byte Breath = 0x02;
		public const byte Foot = 0x04;
		public const byte PortamentoTime = 0x05;
		public const byte DteMsb = 0x06;
		public const byte Volume = 0x07;
		public const byte Balance = 0x08;
		public const byte Pan = 0x0A;
		public const byte Expression = 0x0B;
		public const byte EffectControl1 = 0x0C;
		public const byte EffectControl2 = 0x0D;
		public const byte General1 = 0x10;
		public const byte General2 = 0x11;
		public const byte General3 = 0x12;
		public const byte General4 = 0x13;
		public const byte BankSelectLsb = 0x20;
		public const byte ModulationLsb = 0x21;
		public const byte BreathLsb = 0x22;
		public const byte FootLsb = 0x24;
		public const byte PortamentoTimeLsb = 0x25;
		public const byte DteLsb = 0x26;
		public const byte VolumeLsb = 0x27;
		public const byte BalanceLsb = 0x28;
		public const byte PanLsb = 0x2A;
		public const byte ExpressionLsb = 0x2B;
		public const byte Effect1Lsb = 0x2C;
		public const byte Effect2Lsb = 0x2D;
		public const byte General1Lsb = 0x30;
		public const byte General2Lsb = 0x31;
		public const byte General3Lsb = 0x32;
		public const byte General4Lsb = 0x33;
		public const byte Hold = 0x40;
		public const byte PortamentoSwitch = 0x41;
		public const byte Sostenuto = 0x42;
		public const byte SoftPedal = 0x43;
		public const byte Legato = 0x44;
		public const byte Hold2 = 0x45;
		public const byte SoundController1 = 0x46;
		public const byte SoundController2 = 0x47;
		public const byte SoundController3 = 0x48;
		public const byte SoundController4 = 0x49;
		public const byte SoundController5 = 0x4A;
		public const byte SoundController6 = 0x4B;
		public const byte SoundController7 = 0x4C;
		public const byte SoundController8 = 0x4D;
		public const byte SoundController9 = 0x4E;
		public const byte SoundController10 = 0x4F;
		public const byte General5 = 0x50;
		public const byte General6 = 0x51;
		public const byte General7 = 0x52;
		public const byte General8 = 0x53;
		public const byte PortamentoControl = 0x54;
		public const byte Rsd = 0x5B;
		public const byte Effect1 = 0x5B;
		public const byte Tremolo = 0x5C;
		public const byte Effect2 = 0x5C;
		public const byte Csd = 0x5D;
		public const byte Effect3 = 0x5D;
		public const byte Celeste = 0x5E;
		public const byte Effect4 = 0x5E;
		public const byte Phaser = 0x5F;
		public const byte Effect5 = 0x5F;
		public const byte DteIncrement = 0x60;
		public const byte DteDecrement = 0x61;
		public const byte NrpnLsb = 0x62;
		public const byte NrpnMsb = 0x63;
		public const byte RpnLsb = 0x64;
		public const byte RpnMsb = 0x65;
		// Channel mode messages
		public const byte AllSoundOff = 0x78;
		public const byte ResetAllControllers = 0x79;
		public const byte LocalControl = 0x7A;
		public const byte AllNotesOff = 0x7B;
		public const byte OmniModeOff = 0x7C;
		public const byte OmniModeOn = 0x7D;
		public const byte PolyModeOnOff = 0x7E;
		public const byte PolyModeOn = 0x7F;
	}

}
