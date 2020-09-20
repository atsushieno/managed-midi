using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public abstract class MidiPortCreatorExtension
	{
		public abstract IMidiOutput CreateVirtualInputSender(PortCreatorContext context);
		public abstract IMidiInput CreateVirtualOutputReceiver(PortCreatorContext context);

		public delegate void SendDelegate(byte[] buffer, int index, int length, long timestamp);

		public class PortCreatorContext
		{
			public string ApplicationName { get; set; }
			public string PortName { get; set; }
			public string Manufacturer { get; set; }
			public string Version { get; set; }
		}
	}
}
