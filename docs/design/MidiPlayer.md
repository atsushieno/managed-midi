
# MidiPlayer API

`MidiPlayer` provides fairly straightforward feature for SMF playback.

The constructor takes `IMidiAccess` or `IMidiOutput` instance and a `MidiMusic` instance, which is then used to send MIDI messages in timely manner.

## playback control

There are `Play()`, `Pause()` and `Stop()` methods to control playback state.
MidiPlayer can be used to play the song many times.

There is also `Seek()` method that takes delta time ticks. The implementation is somewhat complicated, see design section later on this page.

## tempo and time

While playing the song, it keeps track of tempo and time signature information from META events.
Raw `Tempo` property value is not very helpful to normal users, so there is also `Bpm` property.

It also provides `PlayDeltaTime` which is the amount of ticks as of current position.
Raw delta time value is not very helpful either, so there is also `PositionInTime` property of `TimeSpan` type.
`PositionInTime` actually involves contextual calculation regarding tempo changes and delta times in the past messages, because conversion from clock counts to TimeSpan requires information on when tempo changes happened.
Therefore this property is not for casual consumption.

There is also `GetTotalPlayTimeMilliseconds()` method which returns the total play time of the song in milliseconds.

MidiPlayer supports fast-forwarding, or slow playback via `TempoChangeRatio` property.

## MIDI event notification

MidiPlayer provides `EventReceived` property that can be used like an event.


# Design notes


## Driver-agnostic MIDI player

MidiPlayer is designed to become platform-agnostic.

MidiPlayer itself does not access platform-specific MIDI outputs.
IMidiAccess and IMidiOutput are the interfaces that provides raw MIDI access, and they are implemented for each platform-supported backends.

Raw MIDI access separation makes it easy to test MIDI player functionality without any platform-specific hussle, especially with `MidiAccessManager.Empty` (NO-OP midi access implementation) and VirtualMidiPlayerTimeManager explained later.


## Format 0

When dealing with sequential MIDI messages, it is much easier if every MIDI events from all the tracks are unified in one sequence.
Therefore MidiPlayer first converts the song to "Format 0" which has only one track.


## SMTPe vs. clock count

MidiPlayer basically doesn't support SMTPe. It is primarily used for serializing MIDI device inputs in real time, not for structured music.
It affects tempo calculation, and so far MidiPlayer aims to provide features for structured music.


## IMidiPlayerTimeManager

One of the annoyance with audio and MIDI API is that they involve real-world time.
If you want to test any code that plays some song that lasts 3 minutes, you don't want to actually wait for 3 minutes.
There should be some fake timer when testing something.

IMidiPlayerTimeManager is designed to make it happen. You can consider it similar to Reactive Extensions "schedulers", which is also designed to make (occasionally) timed streams easily testable.

There is `SimpleAdjustingMidiPlayerTimeManager` which is based on real-world time, and `VirtualMidiPlayerTimeManager` where its users are supposed to manually control time progress.
The latter class provides two time controller methods:

- WaitBy() is called by MidiPlayer internals, to virtually wait for the next event. Call to this method can cause blocking, until the virtual time "proceeds".
- ProceedBy() is called by developers so that MidiPlayer can process next events. This method unblocks the player (caused by WaitBy() calls)


WaitBy() is actually the interface method that every time manager has to implement.

SimpleAdjustingMidiPlayerTimeManager does somewhat complicated tasks beyond mere Task.Delay() - it remembers the last MIDI event and real-world time, and calculates how long it should actually wait for.
It works as a timestamp adjuster. It is important for MIDI players to play the song in exact timing, so someone needs to adjust the time between events.
Programs can delay at any time (especially .NET runtime can pause long time with garbage collectors) and it is inevitable, but this class plays an important role here to minimize the negative impact.


## MidiEventLooper

MidiEventLooper is to process MIDI messages in timely manner.
It's an internal that users have no access. MidiPlayer users it to control
play/pause/stop state, as well as give tempo changes (time stretching).

There could be more than one implementation for the event looper - current implementation "blocks" MIDI message "waits" in possibly real-world time.
To avoid that, we could implement the time manager so that it loops in very short time like per clock count, but it will consume too much computing resource, so we avoided that.
Those who have no power problem (e.g. on desktop PC) might want to have event loopers which is precisely controllable - we might provide choices (not in high priority now).


## ISeekProcessor

MidiPlayer supports seek operation i.e. it can jump to any point specified as in clock count (delta time).

Implementing seek operation is not very simple. THere are couple of requirements.

First, it must mute ongoing notes. Without note-offs, the MIDI output device will keep playing extraneous notes.

Second, it cannot directly jump to the event at the specified time and play, because those MIDI channels may hold different values program changes, control changes, pitchbends and so on.
To reproduce precise values for them, the player first needs to go back to the top of the song, process those events with no time, skipping any note events.

Third, optionally, there will be a bunch of "ignorable" events when processing those events from the top of the song.
Consider pitch bend changes - they quickly changes in very short delta time, can be thousands, but in the end they would reset to zero or some fixed value.
Modern MIDI devices won't be in trouble, but classic MIDI devices may have trouble dealing with thousands of events in milliseconds.

Currently MidiPlayer implements solutions for the first two problems.
The third problem is something that had better be resolved, but so far we're not suffered from it very much.

It is also possible that developers want to control which kind of messages should be processed, especially regarding NRPNs, sysex, and meta events, because they might function like note operations (e.g. "vocaloid" made use of some of those messages).
For such uses, we should rather provide customizible seek processors. It is actually why there is ISeekProcessor interface.
However we are still unsure if current interface API is good for that. Therefore it is not exposed to the public yet. Feedbacks are welcome.

There is also room for `MidiMachine` class that can "remember" statuses of each MIDI channel. It works like a pseudo MIDI device.
