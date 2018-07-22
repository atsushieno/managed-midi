using System;
namespace Commons.Music.Midi
{
	public partial class MidiAccessManager
	{
		partial void InitializeDefault ()
		{
			// What we can support in this implementation assembly: WinMM and ALSA. For Mac, you need Commons.Music.Midi.XamMacFull.csproj.
			Default = Environment.OSVersion.Platform != PlatformID.Unix ? (IMidiAccess) new WinMM.WinMMMidiAccess () : new Alsa.AlsaMidiAccess ();
		}
	}
}
