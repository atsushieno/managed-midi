# managed-midi

managed-midi aims to provide C#/.NET API For almost-raw access to MIDI devices in cross-platform manner with the greatest common measure so that it can be "commons" either on Mono or .NET, everywhere, as well as standard MIDI file manipulation and player functionality.

In particular, this library is used by these projects:

- https://github.com/atsushieno/mugene/ - music macro language (MML) compiler for SMF
- https://github.com/atsushieno/xmdsp - visual MIDI player for MML-based composers
- https://github.com/atsushieno/fluidsynth-midi-service - virtual MIDI synthesizer for Android
- https://github.com/atsushieno/xmmk - cross platform visual MIDI keyboard using your PC keyboard.

(Right now it is totally @atsushieno's own project and those use cases are all by myself. I hope to be able to split out "showcase" section if there are any other uses!)



## What is special about managed-midi?

managed-midi is truly cross-platform oriented. The true cross platform means it supports Linux, Mac, Windows classic and UWP, iOS and Android. There is no one else which actually tries to achieve that.

(iOS is untested. They are just that the API implementations exist. But it should be almost identical to Xamarin.Mac.)

Mono, .NET Framework and .NET Core are the supported frameworks.


## Issues

Check our github [issues](https://github.com/atsushieno/managed-midi/issues) for present issues. We appreciate your bug reports there too.


## API

On API stability: The entire API is still subject to change. Historically there has been a lot of breaking changes in very significant manner.

We will start following semantic versioning 2.0.0 scheme at some point. For now minor version changes may result in incompatible API changes.

API metadata (assemblies, namespaces, types etc.) can be browsed at https://fuget.org/packages/managed-midi/ (thanks to fuget.org)


## Quick feature survey

Here is the list of the base library features:

- `MidiEvent`, `MidiMessage`, `MidiTrack` and `MidiMusic` to store sequence of events, tracks, up to a song.
  - No strongly-typed message types (something like NoteOnMessage, NoteOffMessage, and so on). There is no point of defining strongly-typed messages for each mere MIDI status byte - you wouldn't need message type abstraction.
  - No worries, there are `MidiCC`, `MidiRpnType`, `MidiMetaType` and `MidiEvent` fields (of type System.Byte) so that you don't have to remember the actual numbers.
- `SmfReader` and `SmfWriter`: read and write SMF (standard MIDI format) files with MIDI messages.
  - There are also `SmfTrackMerger` and `SmfTrackSplitter` that helps you implementing sequential event processing for your own MIDI players, or per-track editors.
- `IMidiAccess`: raw MIDI Access abstraction, to create `IMidiInput` and `IMidiOutput` channels that are used to receive or send MIDI messages to and from the actual MIDI devices.
  - They are implemented for many platform specific MIDI API within this library, and you can implement your own backend if you need.
- MidiPlayer:
  - It supports play/pause/stop and fast-forwarding.
  - Midi messages are sent to its event `EventReceived`. If you don't pass a Midi Access instance or a Midi Output instance, it will do nothing.
  - `IMidiTimeManager`: Time manager is abstract. You can define your actual behavior for "advance by X seconds". By default it (of course) waits for the specified time, using `Task.Delay()`. It's like IScheduler in Rx.
- `MidiMachine`: it represents a virtual output device that updates its status such as Program numbers, RPNs, NRPNs, PAfs, CAfs, and ptchbends, from all the inputs it has received. It would be useful to display current statuses for a MIDI device while playing some song.
- `MidiModuleDatabase`: it stores sets of instrument program-bank-name mappings that helps you display the instruments that are being played. They will be useful when you are implementing visual MIDI players.
  - The library contains some built-in device information in JSON format.

Basically, these features with raw MIDI access implementation makes up this managed-midi library.



## MIDI Access API implementations

### ALSA

This is the primary Linux MIDI support and the actual ALSA implementation is done by [alsa-sharp](https://github.com/atsushieno/alsa-sharp) project.

### WinMM

This is the most reliable implemetation for Windows desktop.

We needed this to create Xwt-based projects which depend on WPF.

### UWP

Almost untested. We need some app beyond proof of concept SMF player.

### CoreMidiApi

Almost untested. We need some MIDI devices that work fine on our Mac environment. We never tried with iOS device yet.

### RtMidiSharp

RtMidi is a cross-platform C and C++ library for raw MIDI access: https://github.com/thestk/rtmidi

managed-midi supports rtmidi through a component called RtMidiSharp (and wrapper around our own common API called RtMidiAccess).

Having said that, managed-midi for Windows is only known to work on 32bit environment.
It is most likely because of the native dependencies (portmidi or rtmidi).
Any contribution to get it working on 64bit Windows environment is welcome.

For OSX and Linux only 64bit binaries are included.

(It used to be provided by a fork of rtmidi called rtmidi-c (https://github.com/atsushieno/rtmidi-c) but it is now merged to the upstream.)

### PortMidiSharp

PortMidi is another cross-platform C library for raw MIDI access: http://portmedia.sourceforge.net/

There are some binaries included, but we're not sure if it still works (it was built many years ago with old platforms). You are encouraged to build and provide your own version.

It was actually the first MIDI API that managed-midi supported and almost untouched since then (except that we offer the common API called PortMidiAccess).

### Create your own MIDI Access API

With IMidiAccess interface anyone can write own MIDI access implementation.

For example, [nfluidsynth](https://github.com/atsushieno/nfluidsynth) is a .NET binding to `(lib)fluidsynth` and has an implementation that makes use of it.


## Quick Examples

Also, see [tools](https://github.com/atsushieno/managed-midi/tree/master/tools/) directory for live use cases.

When trying them below, C# shell is useful: `csharp -r Commons.Music.Midi.Desktop.dll`

### Play notes

Make sure that you have active and audible (i.e. non-thru) MIDI output device.

```csharp
using Commons.Music.Midi;

var access = MidiAccessManager.Default;
var output = access.OpenOutputAsync(access.Outputs.Last().Id).Result;
output.Send(new byte [] {0xC0, GeneralMidi.Instruments.AcousticGrandPiano}, 0, 2, 0); // There are constant fields for each GM instrument
output.Send(new byte [] {MidiEvent.NoteOn, 0x40, 0x70}, 0, 3, 0); // There are constant fields for each MIDI event
output.Send(new byte [] {MidiEvent.NoteOff, 0x40, 0x70}, 0, 3, 0);
output.Send(new byte [] {MidiEvent.Program, 0x30}, 0, 2, 0); // Strings Ensemble
output.Send(new byte [] {0x90, 0x40, 0x70}, 0, 3, 0);
output.Send(new byte [] {0x80, 0x40, 0x70}, 0, 3, 0);
output.CloseAsync();
```

### Play MIDI song file (SMF), detecting specific events

```csharp
using Commons.Music.Midi;

var access = MidiAccessManager.Default;
var output = access.OpenOutputAsync(access.Outputs.Last().Id).Result;
var music = MidiMusic.Read(System.IO.File.OpenRead("mysong.mid"));
var player = new MidiPlayer(music, output);
player.EventReceived += (MidiEvent e) => {
  if (e.EventType == MidiEvent.Program)
    Console.WriteLine ($"Program changed: Channel:{e.Channel} Instrument:{e.Msb}");
  };
player.PlayAsync();
Console.WriteLine("Type [CR] to stop.");
Console.ReadLine();
player.Dispose();
```

## HACKING

### The library structure

<del>The current version of managed-midi is designed to be packaged as a NuGet library using NuGetizer 3000: https://github.com/NuGet/NuGet.Build.Packaging</del>I cannot build both CoreMidi (XamMac) and UWP at the same time, so it will be packaged manually.

There is no reference assembly; the netstandrd2.0 library is part of the package, which contains *no* raw MIDI API access implementation. It can still be used to implement platform-specific API on top of it.

While there is netstandard2.0 version, there is a shared library version of the most of the common API and data set. netstandard2.0 version is just a project that wraps around it. Other assembllies such as Desktop, CoreMidi, Android and UWP versions use this shared project.

This project structure is done so that we can easily hack any part on any platform (especially on Linux).
