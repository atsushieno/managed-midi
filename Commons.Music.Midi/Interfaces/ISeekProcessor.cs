using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public enum SeekFilterResult
	{
		Pass,
		Block,
		PassAndTerminate,
		BlockAndTerminate,
	}

	public interface ISeekProcessor
	{
		SeekFilterResult FilterMessage(MidiMessage message);
	}
	
}
