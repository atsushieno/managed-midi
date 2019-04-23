using System;
namespace Commons.Music.Midi.WinMM
{
	[Obsolete ("This class does not do anything special. Just use MidiPlayer.")]
	public class WinMMMidiPlayer : MidiPlayer
	{
		public WinMMMidiPlayer (MidiMusic music)
			: base (music)
		{
		}
	}
}
