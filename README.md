# managed-midi

managed-midi aims to provide C#/.NET API For almost-raw access to MIDI devices in cross-platform manner with the greatest common measure so that it can be "commons" either on Mono or .NET, everywhere, as well as standard MIDI file manipulation and player functionality.

In particular, this library is used by these projects:

- https://github.com/atsushieno/mugene/ - music macro language (MML) compiler for SMF
- https://github.com/atsushieno/xmdsp - visual MIDI player for MML-based composers
- https://github.com/atsushieno/fluidsynth-midi-service - virtual MIDI synthesizer for Android
- https://github.com/atsushieno/xmmk - cross platform visual MIDI keyboard using your PC keyboard.


## What makes it special

managed-midi is truly cross-platform oriented. The true cross platform means it supports Linux, Mac, Windows classic and UWP, iOS and Android. There is no one else which actually tries to achieve that.

(Mac, iOS and UWP are untested. They are just that the API implementations exist.)


## API stability

The entire API is subject to change. Historically there has been a lot of breaking changes in very significant manner.


## The library structure

The current version of managed-midi is designed to be packaged as a NuGet library using NuGetizer 3000: https://github.com/NuGet/NuGet.Build.Packaging

However there is no reference assembly; the PCL package is part of the package, which contains *no* raw MIDI API access implementation. It can still be used to implement platform-specific API.

While there is PCL version, there is a shared library version of the most of the common API and data set. PCL version is just a project that wraps around it. Other assembllies such as Desktop, CoreMidi, Android and UWP versions use this shared project.

This project structure is done so that we can easily hack any part on any platform (especially on Linux).


## MIDI Access API implementations

### RtMidiSharp

RtMidi is a cross-platform C and C++ library for raw MIDI access: https://github.com/thestk/rtmidi

managed-midi supports rtmidi through a component called RtMidiSharp (and wrapper around our own common API called RtMidiAccess).

Having said that, Windows build is known to work only on 32bit environment.
It is most likely because of the native dependencies (portmidi or rtmidi).
Any contribution to get it working on 64bit Windows environment is welcome.

And for OSX and Linux only 64bit binaries are included.

(It used to be provided by a fork of rtmidi called rtmidi-c (https://github.com/atsushieno/rtmidi-c) but it is now merged to the upstream.)

### PortMidiSharp

PortMidi is another cross-platform C library for raw MIDI access: http://portmedia.sourceforge.net/

There are some binaries included, but we're not sure if it still works (it was built many years ago with old platforms). You are encouraged to build and provide your own version.

It was actually the first MIDI API that managed-midi supported and almost untouched since then (except that we offer the common API called PortMidiAccess).

### WinMM

This is the most reliable implemetation for Windows desktop.

We needed this to create Xwt-based projects which depend on WPF.

### UWP

(Totally untested. We don't have any app with this API that supports UWP yet.)

### CoreMidiApi

(Totally untested. We need some MIDI devices that work fine on our Mac environment.)

