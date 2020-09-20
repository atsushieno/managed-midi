using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media.Midi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Commons.Music.Midi.Droid
{
	public class MidiInput : MidiPort, IMidiInput
	{
		MidiOutputPort port;
		Receiver receiver;

		public MidiInput(MidiPortDetails details, MidiOutputPort port)
			: base(details, () => port.Close())
		{
			this.port = port;
			receiver = new Receiver(this);
			port.Connect(receiver);
		}

		internal override void Close()
		{
			port.Disconnect(receiver);
			base.Close();
		}

		class Receiver : MidiReceiver
		{
			MidiInput parent;

			public Receiver(MidiInput parent)
			{
				this.parent = parent;
			}

			public override void OnSend(byte[] msg, int offset, int count, long timestamp)
			{
				if (parent.MessageReceived != null)
					parent.MessageReceived(this, new MidiReceivedEventArgs()
					{
						Data = offset == 0 && msg.Length == count ? msg : msg.Skip(offset).Take(count).ToArray(),
						Timestamp = timestamp
					});
			}
		}

		public event EventHandler<MidiReceivedEventArgs> MessageReceived;
	}
}