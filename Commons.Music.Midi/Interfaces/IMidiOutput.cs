using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public interface IMidiOutput : IMidiPort, IDisposable
	{
		void Send(byte[] mevent, int offset, int length, long timestamp);
    }

	public static class MidiOutputExtension
    {
        public static void SendMessage(this IMidiOutput output, MidiMessage midiMessageToSend)
        {
            output.Send(midiMessageToSend.RawData, 0, midiMessageToSend.RawData.Length, midiMessageToSend.DeltaTime);
        }
    }
}
