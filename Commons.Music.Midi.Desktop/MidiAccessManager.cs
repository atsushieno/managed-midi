using System;
namespace Commons.Music.Midi
{
	public partial class MidiAccessManager
	{
		partial void InitializeDefault ()
		{
			Default = Environment.OSVersion.Platform != PlatformID.Unix ? (IMidiAccess)new WinMM.WinMMMidiAccess() : new RtMidi.RtMidiAccess();
		}
	}
}
