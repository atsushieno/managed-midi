using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Commons.Music.Midi
{
	public abstract class SimpleVirtualMidiPort : IMidiPort
	{
		IMidiPortDetails details;
		Action on_dispose;
		MidiPortConnectionState connection;

		protected SimpleVirtualMidiPort(IMidiPortDetails details, Action onDispose)
		{
			this.details = details;
			on_dispose = onDispose;
			connection = MidiPortConnectionState.Open;
		}

		public IMidiPortDetails Details => details;

		public MidiPortConnectionState Connection => connection;

		public Task CloseAsync()
		{
			return Task.Run(() => {
				if (on_dispose != null)
					on_dispose();
				connection = MidiPortConnectionState.Closed;
			});
		}

		public void Dispose()
		{
			CloseAsync().Wait();
		}
    }
}
