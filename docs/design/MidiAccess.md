
# MidiAccess API

`IMidiAccess` is the interface that provides access to platform-specific
MIDI API through the same type.

On Windows classic desktop it is WinMM, on macOS and iOS it is CoreMIDI
(through Xamarin.Mac so far), on Linux it is ALSA sequencer, on Android
it is Android MIDI API, on UWP it is UWP MIDI API, and so on...

It is also possible to implement this API for cross-platform MIDI access
libraries. In fact managed-midi contains `RtMidiAccess` and `PortMidiAccess`.

It is also possible to implement this API for custom MIDI devices and/or
ports.
There is FluidsynthMidiAccess implemented via [nfluidsynth](https://github.com/atsushieno/nfluidsynth) project.

Lastly, but as often the most useful implementation, there is EmptyMidiAccess implementation that basically does NO-OP. It is the default for netstandard2.0 build.

Speaking of defaults, `MidiAccessManager.Default` property provides the most-likely default providers for each platform, listed above.

# API Design notes

There are various options on how a cross-platform MIDI access API can be designed.
IMidiAccess basically follows what Web MIDI API provides, except that it does not currently track device connection state changes.
If you really need it, you can observe Inputs and Outputs of IMidiAccess using simple timer loop.

