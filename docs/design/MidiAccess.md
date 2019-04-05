
# MidiAccess API

`IMidiAccess` is the platform-agnostic interface that provides access to platform-specific native MIDI API.
.NET has various runtimes and profiles, which are often tied to platforms, for example, UWP for Windows, Xamarin.Mac for macOS, `net461` for both .NET Framework i.e. for Windows and Mono for Linux etc.

managed-midi assemblies are built for multiple platforms, and they are organized as a "bait and switch" NuGet package.

On Windows classic desktop it is WinMM, on macOS and iOS it is CoreMIDI
(through Xamarin.Mac so far), on Linux it is ALSA sequencer, on Android
it is Android MIDI API, on UWP it is UWP MIDI API, and so on...

It is also possible to implement this API for cross-platform MIDI access
libraries. In fact managed-midi contains `RtMidiAccess` and `PortMidiAccess`.

It is also possible to implement this API for custom MIDI devices and/or
ports.
There is `FluidsynthMidiAccess` implemented via [nfluidsynth](https://github.com/atsushieno/nfluidsynth) project.

Lastly, but as often the most useful implementation, there is `EmptyMidiAccess` implementation that basically does NO-OP. It is the default for netstandard2.0 build.

Speaking of defaults, `MidiAccessManager.Default` property provides the most-likely default providers for each platform, listed above.

# Design notes

## Feature set

There are various options on how a cross-platform MIDI access API can be designed.
For now, IMidiAccess basically follows what Web MIDI API provides, except that it does not currently track device connection state changes.
If you really need it, you can observe Inputs and Outputs of IMidiAccess using simple timer loop.

It will be extended to provide functionality to create arbitrary virtual input and output ports. It is doable only with CoreMIDI (Mac/iOS) and Linux (ALSA).
Such "optional" features will be provided as "extension" types.

## asynchronous API

It is argurable that the API should be asynchronous or not. At this state, these operations are exposed as asynchronous on the common interface:

- `IMidiAccess.OpenInputAsync()`
- `IMidiAccess.OpenOutputAsync()`
- `IMidiInput.CloseAsync()`
- `IMidiOutput.CloseAsync()`

Other operations, such as `IMidiOutput.Send()` and `IMidiInput.MessageReceived` are implemented as synchronous.
While they are designed to be synchronous, users shouldn't expect them to "block" until the actual MIDI messaging is done.
They are "fire and forget" style, like UDP messages.

Implementation of these interfaces can be "anything". It is possible, especially when network transport is involved like RTP MIDI, that even those send/receive operations can take a while to complete.
If these methods are designed to be blocked, then applications likely get messed.

On the other hand, if it is designed as Task-based asynchronous API, then users will deal with async/await context.
But for ordinal MIDI access like WinMM or CoreMIDI, they shouldn't take too much time to process. On the other hand, those MIDI messages can be sent in very short time, and in such case tons of awaits only cause extraneous burden on users apps.

For MIDI access implementations like RTP support, their send/receive operations should be still implemented to return immediately, while queuing messages on some other messaging.

For open and close operations, there wouldn't be too many calls and there is no performance concern.
