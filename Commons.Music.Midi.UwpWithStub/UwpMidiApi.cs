using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace System
{
	public static class IAsyncOperationExtensions
	{
		public static Task<T> AsTask<T> (this IAsyncOperation<T> source)
		{
			throw new NotImplementedException ();
		}
	}
}	

namespace System.Runtime.InteropServices.WindowsRuntime
{
	public static class SomethingIDunnoWhichClassItIs
	{
		public static byte [] ToArray (this IBuffer buffer)
		{
			throw new NotImplementedException ();
		}
	}
}

namespace Windows.Devices.Enumeration
{
	public sealed class DeviceInformation// : IDeviceInformation, IDeviceInformation2
	{
		public static IAsyncOperation<DeviceInformationCollection> FindAllAsync (String aqsFilter)
		{
			throw new NotImplementedException ();
		}

		public string Id { get; }
		
		public string Name { get; }
	}

	public sealed class DeviceInformationCollection : IReadOnlyList<DeviceInformation>
	{
		public DeviceInformation this [int index] => throw new NotImplementedException ();

		public int Count => throw new NotImplementedException ();

		public IEnumerator<DeviceInformation> GetEnumerator ()
		{
			throw new NotImplementedException ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			throw new NotImplementedException ();
		}
	}
}

namespace Windows.Foundation
{
	public interface IClosable
	{
		void Close();
	}
	
	public interface IAsyncOperation<TResult>
	{
		AsyncOperationCompletedHandler<TResult> Completed { get; set; }
		
		TResult GetResults();
	}
	
	public delegate void AsyncOperationCompletedHandler<TResult>(IAsyncOperation<TResult> asyncInfo, AsyncStatus asyncStatus);
	
	public enum AsyncStatus
	{
		Started,
		Completed,
		Canceled,
		Error,
	}
	
	public delegate void TypedEventHandler<TSender,TResult>(TSender sender, TResult args);
}

namespace Windows.Storage.Streams
{
	public interface IBuffer
	{
		uint Capacity { get; }
		uint Length { get; set; }
	}
}

namespace Windows.Devices.Midi
{
	public interface IMidiMessage
	{
		IBuffer RawData { get; }
		TimeSpan Timestamp { get; }
		MidiMessageType Type { get; }
	}
	
	public interface IMidiOutPort : IClosable
	{
		string DeviceId { get; }
		void SendBuffer(IBuffer midiData);
		void SendMessage(IMidiMessage midiMessage);
	}
	
	public sealed class MidiActiveSensingMessage : IMidiMessage
	{
		public MidiActiveSensingMessage()
		{
		}
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
	
	public sealed class MidiChannelPressureMessage : /*IMidiChannelPressureMessage,*/ IMidiMessage
	{
		public MidiChannelPressureMessage(byte channel, byte pressure)
		{
		}
		
		public byte Channel { get; }
		
		public byte Pressure { get; }
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
	
	public sealed class MidiContinueMessage : IMidiMessage
	{
		public MidiContinueMessage()
		{
		}
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
	
	public sealed class MidiControlChangeMessage : /*IMidiControlChangeMessage,*/ IMidiMessage
	{
		public MidiControlChangeMessage(byte channel, byte controller, byte controlValue)
		{
		}
		
		public byte Channel { get; }
		
		public byte Controller { get; }
		
		public byte ControlValue { get; }
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
	
	public sealed class MidiInPort : /*IMidiInPort,*/ IDisposable
	{
		public string DeviceId { get; }
		
		public void Close ()
		{
			throw new NotImplementedException ();
		}
		
		public void Dispose()
		{
			throw new NotImplementedException ();
		}
		
		public static IAsyncOperation<MidiInPort> FromIdAsync(String deviceId)
		{
			throw new NotImplementedException ();
		}
		
		public static string GetDeviceSelector()
		{
			throw new NotImplementedException ();
		}
		
		public event TypedEventHandler<MidiInPort,MidiMessageReceivedEventArgs> MessageReceived;
	}
	
	public sealed class MidiMessageReceivedEventArgs// : IMidiMessageReceivedEventArgs
	{
		public IMidiMessage Message { get; }
	}
	
	public enum MidiMessageType
	{
		None = 0,
		ActiveSensing,
		ChannelPressure,
		Continue,
		ControlChange,
		EndSystemExclusive,
		MidiTimeCode,
		NoteOff,
		NoteOn,
		PitchBendChange,
		PolyphonicKeyPressure,
		ProgramChange,
		SongPositionPointer,
		SongSelect,
		Start,
		Stop,
		SystemExclusive,
		SystemReset,
		TimingClock,
		TuneRequest,
	}
	
	public sealed class MidiNoteOffMessage : IMidiMessage//, IMidiNoteOffMessage
	{
		public MidiNoteOffMessage(byte channel, byte note, byte velocity)
		{
			Channel = channel;
			Note = note;
			Velocity = velocity;
		}
		
		public byte Channel { get; private set; }
		
		public byte Note { get; private set; }
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
		
		public byte Velocity { get; private set; }
	}
	
	public sealed class MidiNoteOnMessage : IMidiMessage//, IMidiNoteOnMessage
	{
		public MidiNoteOnMessage(byte channel, byte note, byte velocity)
		{
			Channel = channel;
			Note = note;
			Velocity = velocity;
		}
		
		public byte Channel { get; private set; }
		
		public byte Note { get; private set; }
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
		
		public byte Velocity { get; private set; }
	}

	public sealed class MidiOutPort : IMidiOutPort, IDisposable
	{
		public string DeviceId { get; }
		
		public void Close ()
		{
			throw new NotImplementedException ();
		}
		
		public void Dispose()
		{
			throw new NotImplementedException ();
		}
		
		public static IAsyncOperation<MidiOutPort> FromIdAsync(String deviceId)
		{
			throw new NotImplementedException ();
		}
		
		public static string GetDeviceSelector()
		{
			throw new NotImplementedException ();
		}
		
		public void SendBuffer(IBuffer midiData)
		{
			throw new NotImplementedException ();
		}
		
		public void SendMessage(IMidiMessage midiMessage)
		{
			throw new NotImplementedException ();
		}
	}
	
	public sealed class MidiPitchBendChangeMessage : IMidiMessage//, IMidiPitchBendChangeMessage
	{
		public MidiPitchBendChangeMessage(byte channel, ushort bend)
		{
			Channel = channel;
			Bend = bend;
		}
		
		public ushort Bend { get; private set; }
		
		public byte Channel { get; private set; }
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
	
	public sealed class MidiPolyphonicKeyPressureMessage : IMidiMessage//, IMidiPolyphonicKeyPressureMessage
	{
		public MidiPolyphonicKeyPressureMessage(byte channel, byte note, byte pressure)
		{
			Channel = channel;
			Note = note;
			Pressure = pressure;
		}
		
		public byte Channel { get; private set; }
		
		public byte Note { get; private set; }
		
		public byte Pressure { get; private set; }
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
	
	public sealed class MidiProgramChangeMessage : IMidiMessage//, IMidiProgramChangeMessage
	{
		public MidiProgramChangeMessage(byte channel, byte program)
		{
			Channel = channel;
			Program = program;
		}
		
		public byte Channel { get; private set; }
		
		public byte Program { get; private set; }
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
	
	public sealed class MidiSongPositionPointerMessage : IMidiMessage//, IMidiSongPositionPointerMessage
	{
		public MidiSongPositionPointerMessage(ushort beats)
		{
			Beats = beats;
		}
		
		public ushort Beats { get; private set; }
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
	
	public sealed class MidiSongSelectMessage : IMidiMessage//, IMidiSongSelectMessage
	{
		public MidiSongSelectMessage(byte song)
		{
			Song = song;
		}
		
		public IBuffer RawData { get; }
		
		public byte Song { get; private set; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
	
	public sealed class MidiStartMessage : IMidiMessage
	{
		public MidiStartMessage()
		{
		}
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
	
	public sealed class MidiStopMessage : IMidiMessage
	{
		public MidiStopMessage()
		{
		}
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
	
	public sealed class MidiSynthesizer : IMidiOutPort/*, IMidiSynthesizer*/, IDisposable
	{
		public DeviceInformation AudioDevice { get; }
		
		public string DeviceId { get; }
		
		public double Volume { get; set; }
		
		public void Close ()
		{
			throw new NotImplementedException ();
		}
		
		public static IAsyncOperation<MidiSynthesizer> CreateAsync()
		{
			throw new NotImplementedException ();
		}
		
		public static IAsyncOperation<MidiSynthesizer> CreateAsync(DeviceInformation audioDevice)
		{
			throw new NotImplementedException ();
		}
		
		public void Dispose()
		{
			throw new NotImplementedException ();
		}
		
		public static bool IsSynthesizer(DeviceInformation midiDevice)
		{
			throw new NotImplementedException ();
		}
		
		public void SendBuffer(IBuffer midiData)
		{
			throw new NotImplementedException ();
		}
		
		public void SendMessage(IMidiMessage midiMessage)
		{
			throw new NotImplementedException ();
		}
	}
	
	public sealed class MidiSystemExclusiveMessage : IMidiMessage
	{
		public MidiSystemExclusiveMessage(IBuffer rawData)
		{
			RawData = rawData;
		}
		
		public IBuffer RawData { get; private set; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
	
	public sealed class MidiSystemResetMessage : IMidiMessage
	{
		public MidiSystemResetMessage()
		{
		}
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
	
	public sealed class MidiTimeCodeMessage : IMidiMessage//, IMidiTimeCodeMessage
	{
		public MidiTimeCodeMessage(byte frameType, byte values)
		{
			FrameType = frameType;
			Values = values;
		}
		
		public byte FrameType { get; }
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
		
		public byte Values { get; }
	}
	
	public sealed class MidiTimingClockMessage : IMidiMessage
	{
		public MidiTimingClockMessage()
		{
		}
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
	
	public sealed class MidiTuneRequestMessage : IMidiMessage
	{
		public MidiTuneRequestMessage()
		{
		}
		
		public IBuffer RawData { get; }
		
		public TimeSpan Timestamp { get; }
		
		public MidiMessageType Type { get; }
	}
}
