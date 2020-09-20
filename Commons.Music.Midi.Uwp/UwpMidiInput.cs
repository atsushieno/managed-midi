using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Midi;

namespace Commons.Music.Midi.Uwp
{
	public class UwpMidiInput : IMidiInput
	{
		internal UwpMidiInput(MidiInPort input, UwpMidiPortDetails details)
		{
			this.input = input;
			Details = details;
			Connection = MidiPortConnectionState.Open;
			input.MessageReceived += DispatchMessageReceived;
		}

		MidiInPort input;

		public IMidiPortDetails Details { get; private set; }

		public MidiPortConnectionState Connection { get; private set; }

		public event EventHandler<MidiReceivedEventArgs> MessageReceived;

		void DispatchMessageReceived(MidiInPort port, MidiMessageReceivedEventArgs args)
		{
			MidiMessageType type = args.Message.Type;
			var data = args.Message.RawData.ToArray();
			MessageReceived(this, new MidiReceivedEventArgs { Data = data, Start = 0, Length = data.Length, Timestamp = (long)args.Message.Timestamp.TotalMilliseconds });
		}

		public async Task CloseAsync()
		{
			Connection = MidiPortConnectionState.Pending;
			await Task.Run(() => {
				input.Dispose();
				Connection = MidiPortConnectionState.Closed;
			});
		}

		public void Dispose()
		{
			input.MessageReceived -= DispatchMessageReceived;
			CloseAsync().Wait();
		}
	}
}
