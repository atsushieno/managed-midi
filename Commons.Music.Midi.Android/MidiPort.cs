using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Commons.Music.Midi.Droid;

namespace Commons.Music.Midi.Droid
{
	public class MidiPort : IMidiPort
	{
		MidiPortDetails details;
		MidiPortConnectionState connection;
		Action on_close;

		protected MidiPort(MidiPortDetails details, Action onClose)
		{
			this.details = details;
			on_close = onClose;
			connection = MidiPortConnectionState.Open;
		}

		public MidiPortConnectionState Connection
		{
			get { return connection; }
		}

		public IMidiPortDetails Details
		{
			get { return details; }
		}

		public Task CloseAsync()
		{
			return Task.Run(() => { Close(); });
		}

		public void Dispose()
		{
			Close();
		}

		internal virtual void Close()
		{
			on_close();
			connection = MidiPortConnectionState.Closed;
		}
	}
}