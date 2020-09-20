using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public class SimpleVirtualMidiOutput : SimpleVirtualMidiPort, IMidiOutput
	{
		public SimpleVirtualMidiOutput(IMidiPortDetails details, Action onDispose)
		: base(details, onDispose)
		{
		}

		public MidiPortCreatorExtension.SendDelegate OnSend { get; set; }

		public void Send(byte[] mevent, int offset, int length, long timestamp)
		{
			OnSend?.Invoke(mevent, offset, length, timestamp);
		}        
    }
}
