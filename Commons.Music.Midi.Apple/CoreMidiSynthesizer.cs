using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AVFoundation;
using Foundation;
using UIKit;

#if __MOBILE__
namespace Commons.Music.Midi.iOS
#else
namespace Commons.Music.Midi.macOS
#endif
{
    public class CoreMidiSynthesizer:CoreMidiOutput, IMidiPortDetails
    {
        private AVAudioEngine _engine;
        private AVAudioUnitSampler _sampler;
        private AVAudioSequencer _sequencer;
        const byte kAUSampler_DefaultBankLSB = 0x00;
        const byte kAUSampler_DefaultMelodicBankMSB = 0x79;
        const byte kAUSampler_DefaultPercussionBankMSB = 0x78;

        public string Id => "MuseScore_General-9CCB03A7-24CD-47DA-BDFE-A282A16DB656";

        public string Manufacturer => "S. Christian Collins";

        public string Name => "MuseScore General Midi";

        public string Version => "0.2";

        public CoreMidiSynthesizer()
        {
            details = this;
            _engine = new AVAudioEngine();
            _sampler = new AVAudioUnitSampler();
            _engine.AttachNode(_sampler);
            _engine.Connect(_sampler, _engine.MainMixerNode, format: new AVAudioFormat(44100,1));

            LoadSoundFontIntoSampler(0);
            AddObservers();
            StartEngine();
            SetSessionPlayback();
        }

        private void SetSessionPlayback()
        {
            var audioSession = AVAudioSession.SharedInstance();
            NSError error = audioSession.SetCategory(AVAudioSessionCategory.Playback, AVAudioSessionCategoryOptions.MixWithOthers);
            if (error != null)
            {
                Debug.WriteLine("couldn't set category:");
                Debug.WriteLine(error.ToString());
            }
            else
            {
                error = audioSession.SetActive(true);
                if (error != null)
                {
                    Debug.WriteLine("couldn't set category active:");
                    Debug.WriteLine(error.ToString());
                }
            }
        }

        private void StartEngine()
        {
            if (_engine.Running) {
                Debug.WriteLine("audio engine already started");
                return;
            }
            NSError error;
            _engine.StartAndReturnError(out error);

            if (error != null && error.Code != 0)
            {
                Debug.WriteLine(error.ToString());
            }
        }
        private void AddObservers()
        {
           // throw new NotImplementedException();
        }

        public void LoadSoundFontIntoSampler(byte preset)
        {

            NSUrl bankURL = MidiSystem.PathToSoundFont;
            NSError error;


            _sampler.LoadSoundBank(bankURL, program: preset,
                                            bankMSB: kAUSampler_DefaultMelodicBankMSB,
                                            bankLSB: kAUSampler_DefaultBankLSB,
                                            out error);

            if (error != null && error.Code != 0)
            {
                Debug.WriteLine("error loading sound bank instrument");
            }
        }

        public override void Send(byte[] mevent, int offset, int length, long timestamp)
        {
            int end = offset + length;
            for (int i = offset; i < end;)
            {
                switch (mevent[i] & 0xF0)
                {
                    case MidiEvent.ActiveSense:
                        break;
                    case MidiEvent.NoteOn:
                        if (i + 2 < end)
                        _sampler.StartNote(mevent[i + 1], mevent[i + 2], (byte)(mevent[i] & 0x0f));
                        break;
                    case MidiEvent.NoteOff:
                        if (i + 2 < end)
                            _sampler.StopNote(mevent[i + 1], (byte)(mevent[i] & 0x0f));
                        break;
                    case MidiEvent.PAf:
                        if (i + 2 < end)
                            _sampler.SendPressureForKey(mevent[i + 1], mevent[i + 2], (byte)(mevent[i] & 0x0f));
                        break;
                    case MidiEvent.CC:
                        if (i + 2 < end)
                            _sampler.SendController(mevent[i + 1], mevent[i + 2], (byte)(mevent[i] & 0x0f));
                        break;
                    case MidiEvent.Program:
                        if (i + 2 < end)
                            _sampler.SendProgramChange(mevent[i + 1], (byte)(mevent[i] & 0x0f));
                        break;
                    case MidiEvent.CAf:
                        if (i + 2 < end)
                            _sampler.SendPressure(mevent[i + 1], (byte)(mevent[i] & 0x0f));
                        break;
                    case MidiEvent.Pitch:
                        if (i + 2 < end)
                            _sampler.SendPitchBend((ushort)((mevent[i + 1] << 13) + mevent[i + 2]),(byte)(mevent[i] & 0x0f));
                        break;
                    case MidiEvent.SysEx1:
                        int pos = Array.IndexOf(mevent, MidiEvent.EndSysEx, i, length - i);
                        if (pos >= 0)
                        {
                            pos++;
                            byte[] message = new byte[pos - i];
                            Array.Copy(mevent, i, message, 0, pos - i);
                            _sampler.SendMidiSysExEvent(NSData.FromArray(message));
                            i = pos;
                        }
                        break;
                    default:
                        throw new NotSupportedException($"MIDI message byte '{mevent[i].ToString("X02")}' is not supported.");
                }
                if (mevent[i] != MidiEvent.SysEx1)
                {
                    i += MidiEvent.FixedDataSize(mevent[i]) + 1;
                }
            }
        }
    }
}