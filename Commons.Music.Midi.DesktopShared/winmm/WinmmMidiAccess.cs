using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Commons.Music.Midi.WinMM
{
	public class WinMMMidiAccess : IMidiAccess
	{
		public IEnumerable<IMidiPortDetails> Inputs
		{
			get
			{
				int devs = WinMMNatives.midiInGetNumDevs();
				for (uint i = 0; i < devs; i++)
				{
					MidiInCaps caps;
					WinMMNatives.midiInGetDevCaps ((UIntPtr) i, out caps, (uint) Marshal.SizeOf (typeof (MidiInCaps)));
					yield return new WinMMPortDetails (i, caps.Name, caps.DriverVersion);
				}
			}
		}

		public IEnumerable<IMidiPortDetails> Outputs {
			get {
				int devs = WinMMNatives.midiOutGetNumDevs ();
				for (uint i = 0; i < devs; i++) {
					MidiOutCaps caps;
					var err = WinMMNatives.midiOutGetDevCaps ((UIntPtr) i, out caps, (uint) Marshal.SizeOf (typeof (MidiOutCaps)));
                    if (err != 0)
                        throw new Win32Exception (err);
					yield return new WinMMPortDetails (i, caps.Name, caps.DriverVersion);
				}
			}
		}

		public event EventHandler<MidiConnectionEventArgs> StateChanged;

		public Task<IMidiInput> OpenInputAsync(string portId)
		{
			var details = Inputs.FirstOrDefault(d => d.Id == portId);
			if (details == null)
				throw new InvalidOperationException($"The device with ID {portId} is not found.");
			return Task.FromResult((IMidiInput)new WinMMMidiInput(details));
		}

		public Task<IMidiOutput> OpenOutputAsync(string portId)
		{
			var details = Outputs.FirstOrDefault(d => d.Id == portId);
			if (details == null)
				throw new InvalidOperationException($"The device with ID {portId} is not found.");
			return Task.FromResult((IMidiOutput)new WinMMMidiOutput(details));
		}
	}

	class WinMMPortDetails : IMidiPortDetails
	{
		public WinMMPortDetails(uint deviceId, string name, int version)
		{
			Id = deviceId.ToString();
			Name = name;
			Version = version.ToString();
		}

		public string Id { get; private set; }

		public string Manufacturer { get; private set; }

		public string Name { get; private set; }

		public string Version { get; private set; }
	}

	class WinMMMidiInput : IMidiInput
	{
        MidiInProc midiInProc;

		public WinMMMidiInput(IMidiPortDetails details)
		{
			Details = details;

            // prevent garbage collection of the delegate
            midiInProc = HandleMidiInProc;

            DieOnError(WinMMNatives.midiInOpen(out handle, uint.Parse(Details.Id), midiInProc,
                IntPtr.Zero, MidiInOpenFlags.Function | MidiInOpenFlags.MidiIoStatus));

            DieOnError(WinMMNatives.midiInStart(handle));

            while (lmBuffers.Count < LONG_BUFFER_COUNT)
            {
                var buffer = new LongMessageBuffer(handle);

                buffer.PrepareHeader();
                buffer.AddBuffer();

                lmBuffers.Add(buffer.Ptr, buffer);
            }

            Connection = MidiPortConnectionState.Open;
		}

        const int LONG_BUFFER_COUNT = 16;

        Dictionary<IntPtr, LongMessageBuffer> lmBuffers = new Dictionary<IntPtr, LongMessageBuffer>();
        
        IntPtr handle;
        object lockObject = new object();
        
		byte[] data2b = new byte[2];
		byte[] data3b = new byte[3];

        void HandleData(IntPtr param1, IntPtr param2)
        {
            var status = (byte)((int)param1 & 0xFF);
            var msb = (byte)(((int)param1 & 0xFF00) >> 8);
            var lsb = (byte)(((int)param1 & 0xFF0000) >> 16);
            var data = MidiEvent.FixedDataSize(status) == 2 ? data3b : data2b;
            data[0] = status;
            data[1] = msb;
            if (data.Length == 3)
                data[2] = lsb;

            MessageReceived(this, new MidiReceivedEventArgs() { Data = data, Start = 0, Length = data.Length, Timestamp = (long)param2 });
        }

        void HandleLongData(IntPtr param1, IntPtr param2)
        {
            byte[] data = null;

            lock (lockObject)
            {
                var buffer = lmBuffers[param1];
                data = new byte[buffer.Header.BytesRecorded];

                Marshal.Copy(buffer.Header.Data, data, 0, buffer.Header.BytesRecorded);

                if (Connection == MidiPortConnectionState.Open)
                {
                    buffer.Recycle();
                }
                else
                {
                    lmBuffers.Remove(buffer.Ptr);
                    buffer.Dispose();
                }
            }

            if (data != null && data.Length != 0)
                MessageReceived(this, new MidiReceivedEventArgs() { Data = data, Start = 0, Length = data.Length, Timestamp = (long)param2 });
        }

        void HandleMidiInProc(IntPtr midiIn, MidiInMessage msg, IntPtr instance, IntPtr param1, IntPtr param2)
		{
			if (MessageReceived != null)
			{
                switch (msg)
                {
                    case MidiInMessage.Data:
                        HandleData(param1, param2);
                        break;

                    case MidiInMessage.LongData:
                        HandleLongData(param1, param2);
                        break;

                    case MidiInMessage.MoreData:
                        // TODO input too slow, handle.
                        break;
                    
                    case MidiInMessage.Error:
                        throw new InvalidOperationException($"Invalid MIDI message: {param1}");

                    case MidiInMessage.LongError:
                        throw new InvalidOperationException("Invalid SysEx message.");

                    default:
                        break;
                }
			}
		}

		public IMidiPortDetails Details { get; private set; }

		public MidiPortConnectionState Connection { get; private set; }

		public event EventHandler<MidiReceivedEventArgs> MessageReceived;

		public Task CloseAsync()
		{
			return Task.Run(() =>
			{
                lock (lockObject)
                {
                    Connection = MidiPortConnectionState.Pending;

                    DieOnError(WinMMNatives.midiInReset(handle));
                    DieOnError(WinMMNatives.midiInStop(handle));
                    DieOnError(WinMMNatives.midiInClose(handle));

                    // wait for the device driver to hand back the long buffers through HandleMidiInProc

                    for (int i = 0; i < 1000; i++)
                    {
                        lock (lockObject)
                        {
                            if (lmBuffers.Count < 1)
                                break;
                        }

                        Thread.Sleep(10);
                    }

                    Connection = MidiPortConnectionState.Closed;
                }
            });
		}

		public void Dispose()
		{
            CloseAsync().Wait();
		}

        static void DieOnError(int code)
        {
            if (code != 0)
                throw new Win32Exception(code, $"{WinMMNatives.GetMidiInErrorText(code)} ({code})");
        }

        class LongMessageBuffer : IDisposable
        {
            public IntPtr Ptr { get; set; } = IntPtr.Zero;
            public MidiHdr Header => (MidiHdr)Marshal.PtrToStructure(Ptr, typeof(MidiHdr));

            IntPtr inputHandle;
            static int midiHdrSize = Marshal.SizeOf(typeof(MidiHdr));

            bool prepared = false;

            public LongMessageBuffer(IntPtr inputHandle, int bufferSize = 4096)
            {
                this.inputHandle = inputHandle;

                var header = new MidiHdr()
                {
                    Data = Marshal.AllocHGlobal(bufferSize),
                    BufferLength = bufferSize,
                };

                try
                {
                    Ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MidiHdr)));
                    Marshal.StructureToPtr(header, Ptr, false);
                }
                catch
                {
                    Free();
                    throw;
                }
            }

            public void PrepareHeader()
            {
                if (!prepared)
                    DieOnError(WinMMNatives.midiInPrepareHeader(inputHandle, Ptr, midiHdrSize));

                prepared = true;
            }

            public void UnPrepareHeader()
            {
                if (prepared)
                    DieOnError(WinMMNatives.midiInUnprepareHeader(inputHandle, Ptr, midiHdrSize));

                prepared = false;
            }

            public void AddBuffer() =>
                DieOnError(WinMMNatives.midiInAddBuffer(inputHandle, Ptr, midiHdrSize));

            public void Dispose()
            {
                Free();
            }

            public void Recycle()
            {
                UnPrepareHeader();
                PrepareHeader();
                AddBuffer();
            }

            void Free()
            {
                UnPrepareHeader();

                if (Ptr != IntPtr.Zero) {
                    Marshal.FreeHGlobal(Header.Data);
                    Marshal.FreeHGlobal(Ptr);
                }
            }
        }
    }

	class WinMMMidiOutput : IMidiOutput
	{
		public WinMMMidiOutput (IMidiPortDetails details)
		{
			Details = details;
			WinMMNatives.midiOutOpen (out handle, uint.Parse (Details.Id), null, IntPtr.Zero, MidiOutOpenFlags.Null);
			Connection = MidiPortConnectionState.Open;
		}

		IntPtr handle;

		public IMidiPortDetails Details { get; private set; }

		public MidiPortConnectionState Connection { get; private set; }

		public Task CloseAsync()
		{
			return Task.Run(() =>
			{
				Connection = MidiPortConnectionState.Pending;
				WinMMNatives.midiOutClose(handle);
				Connection = MidiPortConnectionState.Closed;
			});
		}

		public void Dispose ()
		{
            CloseAsync().Wait();
		}

		public void Send (byte [] mevent, int offset, int length, long timestamp)
		{
			foreach (var evt in MidiEvent.Convert (mevent, offset, length)) {
                if (evt.StatusByte < 0xF0)
                {
                    DieOnError(WinMMNatives.midiOutShortMsg(handle, (uint)(evt.StatusByte + (evt.Msb << 8) + (evt.Lsb << 16))));
                }
                else
                {
                    var header = new MidiHdr();
                    bool prepared = false;
                    IntPtr ptr = IntPtr.Zero;
                    var hdrSize = Marshal.SizeOf(typeof(MidiHdr));

                    try
                    {
                        // allocate unmanaged memory and hand ownership over to the device driver

                        header.Data = Marshal.AllocHGlobal(evt.Data.Length);
                        header.BufferLength = evt.Data.Length;
                        Marshal.Copy(evt.Data, 0, header.Data, header.BufferLength);

                        ptr = Marshal.AllocHGlobal(hdrSize);
                        Marshal.StructureToPtr(header, ptr, false);

                        DieOnError(WinMMNatives.midiOutPrepareHeader(handle, ptr, hdrSize));
                        prepared = true;

                        DieOnError(WinMMNatives.midiOutLongMsg(handle, ptr, hdrSize));
                    }

                    finally
                    {
                        // reclaim ownership and free

                        if(prepared)
                            DieOnError(WinMMNatives.midiOutUnprepareHeader(handle, ptr, hdrSize));

                        if (header.Data != IntPtr.Zero)
                            Marshal.FreeHGlobal(header.Data);

                        if (ptr != IntPtr.Zero)
                            Marshal.FreeHGlobal(ptr);
                    }
                }
			}
		}

        static void DieOnError(int code)
        {
            if (code != 0)
                throw new Win32Exception(code, $"{WinMMNatives.GetMidiOutErrorText(code)} ({code})");
        }
    }
}
