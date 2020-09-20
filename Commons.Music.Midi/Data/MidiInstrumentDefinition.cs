using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Commons.Music.Midi
{

	[DataContract]
	public class MidiInstrumentDefinition
	{
		public MidiInstrumentDefinition()
		{
			Maps = new List<MidiInstrumentMap>();
			DrumMaps = new List<MidiInstrumentMap>();
		}

		public IList<MidiInstrumentMap> Maps { get; private set; }

		public IList<MidiInstrumentMap> DrumMaps { get; private set; }

		[DataMember(Name = "Maps")]
		MidiInstrumentMap[] maps
		{
			get { return Maps.ToArray(); }
			set { Maps = new List<MidiInstrumentMap>(value); }
		}

		[DataMember(Name = "DrumMaps")]
		MidiInstrumentMap[] drumMaps
		{
			get { return DrumMaps.ToArray(); }
			set { DrumMaps = new List<MidiInstrumentMap>(value); }
		}
	}

}
