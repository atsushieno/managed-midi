using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media.Midi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Org.Billthefarmer.Mididriver;

namespace Commons.Music.Midi.Droid
{
    public class MidiSynthDevice : MidiReceiver, IMidiOutput, IMidiPortDetails
    {
        private MidiDriver _midiDriver;
        MidiPortConnectionState _state;
        public MidiSynthDevice()
        {
            _midiDriver = new MidiDriver();
            _state = MidiPortConnectionState.Closed;
        }

        public IMidiPortDetails Details => this;

        public MidiPortConnectionState Connection => _state;

        public string Id => "5C3779DF-72CB-41B8-9B62-0DA097F17C66";

        public string Manufacturer => "Google";

        public string Name => "Sonivox EAS Synthesizer";

        public string Version => "1.18";

        public Task CloseAsync()
        {
            return Task.Run(() =>
            {
                Stop();
            });
        }

        public override void OnSend(byte[] msg, int offset, int count, long timestamp)
        {
            if(_state == MidiPortConnectionState.Open)
            {
                byte[] e = new byte[count];
                Array.Copy(msg, offset, e, 0, count);
                _midiDriver.Write(e);
            }
        }

        public void Start()
        {
            _state = MidiPortConnectionState.Open;
            _midiDriver.Start();
        }

        public void Stop()
        {
            _state = MidiPortConnectionState.Closed;
            _midiDriver.Stop();
        }
    }
}