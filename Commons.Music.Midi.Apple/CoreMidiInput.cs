using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreMidi;

#if __MOBILE__
namespace Commons.Music.Midi.iOS
#else
namespace Commons.Music.Midi.macOS
#endif
{
    public class CoreMidiInput : IMidiInput
	{
		public CoreMidiInput(CoreMidiPortDetails details)
		{
			this.details = details;
			client = new MidiClient("inputclient");
			port = client.CreateInputPort("inputport");
			port.ConnectSource(details.Endpoint);
			port.MessageReceived += OnMessageReceived;
		}

		CoreMidiPortDetails details;
		MidiClient client;
		MidiPort port;

		public IMidiPortDetails Details => details;

		public MidiPortConnectionState Connection => throw new NotImplementedException();

		public event EventHandler<MidiReceivedEventArgs> MessageReceived;

		byte[] dispatch_bytes = new byte[100];

		void OnMessageReceived(object sender, MidiPacketsEventArgs e)
		{
			if (MessageReceived != null)
			{
				foreach (var p in e.Packets)
				{
					if (dispatch_bytes.Length < p.Length)
						dispatch_bytes = new byte[p.Length];
					Marshal.Copy(p.Bytes, dispatch_bytes, 0, p.Length);
					MessageReceived(this, new MidiReceivedEventArgs() { Data = dispatch_bytes, Start = 0, Length = p.Length, Timestamp = p.TimeStamp });
				}
			}
		}

		public Task CloseAsync()
		{
			port.Disconnect(details.Endpoint);
			port.Dispose();
			client.Dispose();
			details.Dispose();
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			CloseAsync().Wait();
		}
	}
}