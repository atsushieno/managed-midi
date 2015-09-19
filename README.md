managed-midi repository is a set of midi manipulation modules, written in
C# for Mono and .NET. It is especially designed to be cross platform.

Having said that, Windows build is known to work only on 32bit environment.
It is most likely because of the native dependencies (portmidi or rtmidi).
Any contribution to get it working on 64bit Windows environment is welcome.

And for OSX and Linux only 64bit binaries are included.

RtMIDI support is actually done through my own fork that adds P/Invoke-able
C functions. See [this rtmidi fork](https://github.com/atsushieno/rtmidi) for details.

Atsushi Eno

