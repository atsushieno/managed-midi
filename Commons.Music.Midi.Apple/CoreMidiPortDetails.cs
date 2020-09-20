using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreMidi;
using Foundation;
using UIKit;

#if __MOBILE__
namespace Commons.Music.Midi.iOS
#else
namespace Commons.Music.Midi.macOS
#endif
{
	public class CoreMidiPortDetails : IMidiPortDetails, IDisposable
	{
		public CoreMidiPortDetails(MidiEndpoint src)
		{
			Endpoint = src;
			Id = src.Name + "__" + src.EndpointName;
			Manufacturer = src.Manufacturer;
			Name = string.IsNullOrEmpty(src.DisplayName) ? src.Name : src.DisplayName;

			try
			{
				Version = src.DriverVersion.ToString();
			}
			catch
			{
				Version = "N/A";
			}
		}

		public MidiEndpoint Endpoint { get; set; }

		public string Id { get; set; }

		public string Manufacturer { get; set; }

		public string Name { get; set; }

		public string Version { get; set; }

		public void Dispose()
		{
			Endpoint?.Dispose();
		}
	}
}