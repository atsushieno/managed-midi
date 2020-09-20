using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace Commons.Music.Midi.Uwp
{
	public class UwpMidiPortDetails : IMidiPortDetails
	{
		private DeviceInformation i;

		public UwpMidiPortDetails(DeviceInformation i)
		{
			this.i = i;			
		}

		public DeviceInformation Device => i;

		public string Id => i.Id;

		public string Manufacturer { get; }

		public string Name => i.Name;

		public string Version { get; }
	}
}
