using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	internal class EmptyMidiPortDetails : IMidiPortDetails
	{
		public EmptyMidiPortDetails(string id, string name)
		{
			Id = id;
			Manufacturer = "dummy project";
			Name = name;
			Version = "0.0";
		}

		public string Id { get; set; }
		public string Manufacturer { get; set; }
		public string Name { get; set; }
		public string Version { get; set; }
	}
}
