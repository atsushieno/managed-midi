using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Media.Midi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util;

namespace Commons.Music.Midi.Droid
{
    [Service(Name = "commons.music.midi.droid.MidiSynthDeviceService", Permission = "android.permission.BIND_MIDI_DEVICE_SERVICE")]
    [IntentFilter(new[] { "android.media.midi.MidiDeviceService" }) ]
    [MetaData("android.media.midi.MidiDeviceService", Resource= "@xml/synth_device_info")]
    public class MidiSynthDeviceService : MidiDeviceService
    {
        private MidiSynthDevice _synthEngine = new MidiSynthDevice();
        private bool _synthStarted = false;
                
        public override void OnCreate()
        { 
            base.OnCreate();
        }

        public override void OnDestroy()
        {
            _synthEngine.Stop();
            base.OnDestroy();
        }
        public override MidiReceiver[] OnGetInputPortReceivers()
        {
           return new MidiReceiver[] { _synthEngine }; 
        }

        public override void OnDeviceStatusChanged(MidiDeviceStatus status)
        {
            if (status.IsInputPortOpen(0) && !_synthStarted)
            {
                _synthEngine.Start();
                _synthStarted = true;
            }
            else if (!status.IsInputPortOpen(0) && _synthStarted)
            {
                _synthEngine.Stop();
                _synthStarted = false;
            }
            base.OnDeviceStatusChanged(status);
        }
    }
}