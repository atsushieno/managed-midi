using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media.Midi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Commons.Music.Midi.Droid
{
	public class MidiPortDetails : IMidiPortDetails
	{
		MidiDeviceInfo device;
		MidiDeviceInfo.PortInfo port;

		public MidiPortDetails(MidiDeviceInfo device, MidiDeviceInfo.PortInfo port)
		{
			if (device == null)
				throw new ArgumentNullException(nameof(device));
			if (port == null)
				throw new ArgumentNullException(nameof(port));
			this.device = device;
			this.port = port;
		}

		public MidiDeviceInfo Device
		{
			get { return device; }
		}

		public MidiDeviceInfo.PortInfo Port
		{
			get { return port; }
		}

		public string Id
		{
			get { return "device" + device.Id + "_port" + port.PortNumber; }
		}

		public string Manufacturer
		{
			get { return device.Properties.GetString(MidiDeviceInfo.PropertyManufacturer); }
		}

		public string Name
		{
			get
			{
				var d = device.Properties.GetString(MidiDeviceInfo.PropertyName) ?? "";
				return d + (d == "" ? "" : " ") + port.Name;
			}
		}

		public string Version
		{
			get { return device.Properties.GetString(MidiDeviceInfo.PropertyVersion); }
		}
	}

}