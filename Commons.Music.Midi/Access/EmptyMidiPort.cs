using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Commons.Music.Midi
{
	internal abstract class EmptyMidiPort : IMidiPort
	{
		Task completed_task = Task.FromResult(false);

		public IMidiPortDetails Details
		{
			get { return CreateDetails(); }
		}
		internal abstract IMidiPortDetails CreateDetails();

		public MidiPortConnectionState Connection { get; private set; }

		public Task CloseAsync()
		{
			// do nothing.
			return completed_task;
		}

		public void Dispose()
		{
		}
	}

}
