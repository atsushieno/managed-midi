using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public class MidiTrack
	{
		public MidiTrack()
			: this(new List<MidiMessage>())
		{
		}

		public MidiTrack(IList<MidiMessage> messages)
		{
			if (messages == null)
				throw new ArgumentNullException("messages");
			this.messages = messages as List<MidiMessage> ?? new List<MidiMessage>(messages);
		}

		List<MidiMessage> messages;

		[Obsolete("No need to use this method, simply use Messages.Add")]
		public void AddMessage(MidiMessage msg)
		{
			messages.Add(msg);
		}

		public IList<MidiMessage> Messages
		{
			get { return messages; }
		}
	}
}
