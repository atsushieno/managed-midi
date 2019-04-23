# managed-midi

managed-midi aims to provide C#/.NET API For almost-raw access to MIDI devices in cross-platform manner with the greatest common measure so that it can be "commons" either on Mono or .NET, everywhere, as well as standard MIDI file manipulation and player functionality.

In particular, this library is used and tested by these projects:

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

Here is a quick list of per-platform backend implementation. **Bold** ones are default.

| target framework | Linux | Mac | Windows | Android | iOS |
|------------------|-------|-----|---------|---------|-----|
| netstandard | **Empty** | **Empty** | **Empty** | **Empty** | **Empty** |
| net45 | **ALSA**, portmidi, rtmidi | **own CoreMIDI** (incomplete), portmidi, rtmidi | **WinMM**, portmidi, rtmidi | - | - |
| netcoreapp2.1 | **ALSA**, portmidi, rtmidi | **own CoreMidi** (incomplete), portmidi, rtmidi | **WinMM**, portmidi, rtmidi | - | - |
| MonoAndroid | - | - | - | **Android MIDI API** | - |
| XamariniOS | - | - | - | - | **Xamarin.iOS CoreMIDI** |
| XamarinMac | - | **Xamarin.Mac CoreMIDI** | - | - | - |
| uap10.0 | - | - | **UWP MIDI API** | - | - |

`own CoreMIDI` is a Xamarin-compatible implementation within this repository. It is implemented here to avoid extra dependencies on Xamarin assemblies. They are different from `Xamarin.iOS CoreMIDI` and `Xamarin.Mac CoreMIDI`.

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

So far, managed-midi for Windows is only known to work on 32bit environment.
It may or may not work.
Any contribution to verify and possibly fix any issue on 64bit Windows is welcome.

Note that you need a working native build of rtmidi.dll, librtmidi.dylib or librtmidi.so (depending on the platform).
We don't offer prebuilt binaries for that now.

(This driver used to be provided by a fork of rtmidi called rtmidi-c (https://github.com/atsushieno/rtmidi-c) but it is now merged to the upstream.)

### PortMidiSharp

PortMidi is another cross-platform C library for raw MIDI access: http://portmedia.sourceforge.net/

There are some binaries included, but we're not sure if it still works (it was built many years ago with old platforms). You are encouraged to build and provide your own version.

It was actually the first MIDI API that managed-midi supported and almost untouched since then (except that we offer the common API called PortMidiAccess).

### Create your own MIDI Access API

With IMidiAccess interface anyone can write own MIDI access implementation.

For example, [nfluidsynth](https://github.com/atsushieno/nfluidsynth) is a .NET binding to `(lib)fluidsynth` and has an implementation that makes use of it.


## Quick Examples

Also, see [tools](https://github.com/atsushieno/managed-midi/tree/master/tools/) directory for live use cases.

When trying them below, C# shell is useful: `csharp -r Commons.Music.Midi.dll`

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

### The library and project structures

It is kind of a "bait-and-switch" nuget package. However there is no reference assembly; the netstandrd2.0 library is part of the package, which contains *no* raw MIDI API access implementation. It can still be used to implement platform-specific API on top of it.

There are many projects (in terms of `.csproj`) in `managed-midi.sln`:

- managed-midi.sln
  - Commons.Music.Midi.Shared.csproj - the most common shared library project
  - Commons.Music.Midi.csproj - netstandard 2.0 **implementation**
  - Commons.Music.Midi.DesktopShared.csproj - almost the same as desktop implementation, but shared library project. Used by the following projects
    - Commons.Music.Midi.Desktop.csproj - .NET Framework (net4x) **implementation**
    - Commons.Music.Midi.DotNetCore.csproj - .NET Core (netcoreapp2x) **implementation**
  - Commons.Music.Midi.CoreMidiShared.csproj - shared library project used by iOS and XamMac (full).
  - Commons.Music.Midi.iOS.csproj - Xamarin.iOS **implementation**
  - Commons.Music.Midi.XamMac.csproj - Xamarin.Mac modern profile **implementation**
  - Commons.Music.Midi.Android.csproj - Xamarin.Android **implementation**
  - Commons.Music.Midi.UwpShared.csproj - almost the same as UWP implementation, byt shared library project. (Used by Uwp, and "UwpWithStub" which builds on Linux but implementation is useless)
  - Commons.Music.Midi.Uwp.csproj - UWP **implementation**.

Apart from managed-midi.sln, there is another consolidated project:

- Commons.Music.Midi.XamMacFull.csproj - Xamarin.Mac full profile **implementation** (this cannot be unified with the above because it adds Xamarin.Mac references that cannot be resolved on Linux)

(Note that all those implementation assemblies share the identical name `Commons.Music.Midi.dll` regardless of the project names, by nature of bait-and-switch NuGet package.)

While there is a netstandard2.0 version, there is a shared library version of the most of the common API and data set. netstandard2.0 version is a project that wraps around it. Other assembllies such as .NET Core (netcoreapp2.0), .NET Desktop (net4x), CoreMidi, Android and UWP versions use this shared project.

This project structure is done so that we can easily hack any part on any platform (especially on Linux).

NuGet packaging is manually done at https://atsushieno.visualstudio.com/managed-midi . I cannot locally do that due to Xamarin.Mac and UWP, which Microsoft never supported on Linux (that might change once Microsoft releases UWP sources).


### Implementation notes

There are couple of design note docs placed under [docs](./docs) directory.
