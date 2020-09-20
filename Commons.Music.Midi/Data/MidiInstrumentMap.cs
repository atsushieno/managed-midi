using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Commons.Music.Midi
{
	[DataContract]
	public class MidiInstrumentMap
	{
		public MidiInstrumentMap()
		{
			Programs = new List<MidiProgramDefinition>();
		}

		[DataMember]
		public string Name { get; set; }

		public IList<MidiProgramDefinition> Programs { get; private set; }

		[DataMember(Name = "Programs")]
		MidiProgramDefinition[] programs
		{
			get { return Programs.ToArray(); }
			set { Programs = new List<MidiProgramDefinition>(value); }
		}
	}
}
