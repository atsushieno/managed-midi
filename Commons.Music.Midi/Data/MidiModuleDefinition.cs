using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Commons.Music.Midi
{
	[DataContract]
	public class MidiModuleDefinition
	{
		public MidiModuleDefinition()
		{
			Instrument = new MidiInstrumentDefinition();
		}

		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public string Match { get; set; }

		[DataMember]
		public MidiInstrumentDefinition Instrument { get; set; }

		// serialization

		public void Save(Stream stream)
		{
			var ds = new DataContractJsonSerializer(typeof(MidiModuleDefinition));
			ds.WriteObject(stream, this);
		}

		public static MidiModuleDefinition Load(Stream stream)
		{
			var ds = new DataContractJsonSerializer(typeof(MidiModuleDefinition));
			return (MidiModuleDefinition)ds.ReadObject(stream);
		}
	}


}
