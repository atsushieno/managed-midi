using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Commons.Music.Midi
{

	[DataContract]
	public class MidiProgramDefinition
	{
		public MidiProgramDefinition()
		{
			Banks = new List<MidiBankDefinition>();
		}

		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public int Index { get; set; }


		public IList<MidiBankDefinition> Banks { get; private set; }

		[DataMember(Name = "Banks")]
		MidiBankDefinition[] banks
		{
			get { return Banks.ToArray(); }
			set { Banks = new List<MidiBankDefinition>(value); }
		}
	}

}
