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

namespace Commons.Music.Midi.Driod
{
    class MidiDeviceCallback : MidiManager.DeviceCallback
    {
        public event EventHandler<MidiDeviceInfo>   DeviceAdded;
        public event EventHandler<MidiDeviceInfo>   DeviceRemoved;
        public event EventHandler<MidiDeviceStatus> DeviceStatusChanged;

        public override void OnDeviceAdded(MidiDeviceInfo device)
        {
            DeviceAdded?.Invoke(this, device);

            base.OnDeviceAdded(device);        
        }

        public override void OnDeviceRemoved(MidiDeviceInfo device)
        {
            DeviceRemoved?.Invoke(this, device);

            base.OnDeviceRemoved(device);
        }

        public override void OnDeviceStatusChanged(MidiDeviceStatus status)
        {
            DeviceStatusChanged?.Invoke(this, status);

            base.OnDeviceStatusChanged(status);
        }
    }
}