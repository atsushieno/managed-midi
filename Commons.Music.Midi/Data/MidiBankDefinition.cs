using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Commons.Music.Midi
{
	[DataContract]
	public class MidiBankDefinition
	{
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public int Msb { get; set; }
		[DataMember]
		public int Lsb { get; set; }
	}
}
