using System;
using System.Linq;
using System.Runtime.InteropServices;

using RtMidiPtr = System.IntPtr;
using RtMidiInPtr = System.IntPtr;
using RtMidiOutPtr = System.IntPtr;
using System.Collections.Generic;


namespace Commons.Music.Midi.RtMidi
{
	public enum RtMidiApi
	{
		Unspecified,
		MacOsxCore,
		LinuxAlsa,
		UnixJack,
		WindowsMultimediaMidi,
		WindowsKernelStreaming,
		RtMidiDummy,
	}

	public enum RtMidiErrorType
	{
		Warning,
		DebugWarning,
		Unspecified,
		NoDevicesFound,
		InvalidDevice,
		MemoryError,
		InvalidParameter,
		InvalidUse,
		DriverError,
		SystemError,
		ThreadError,
	}

	public unsafe delegate void RtMidiCCallback (double timestamp, byte* message, long size, IntPtr userData);

	public static class RtMidi
	{
		public const string RtMidiLibrary = "rtmidi";

		/* Utility API */
		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal int rtmidi_sizeof_rtmidi_api ();

		/* RtMidi API */
		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal int rtmidi_get_compiled_api (ref IntPtr/* RtMidiApi ** */ apis);
		// return length for NULL argument.
		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal void rtmidi_error (RtMidiErrorType type, string errorString);

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal void rtmidi_open_port (RtMidiPtr device, uint portNumber, string portName);

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal void rtmidi_open_virtual_port (RtMidiPtr device, string portName);

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal void rtmidi_close_port (RtMidiPtr device);

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal int rtmidi_get_port_count (RtMidiPtr device);

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		[return:MarshalAs (UnmanagedType.LPStr)]
		static extern internal string rtmidi_get_port_name (RtMidiPtr device, uint portNumber);

		/* RtMidiIn API */
		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal RtMidiInPtr rtmidi_in_create_default ();

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal RtMidiInPtr rtmidi_in_create (RtMidiApi api, string clientName, uint queueSizeLimit);

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal void rtmidi_in_free (RtMidiInPtr device);

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal RtMidiApi rtmidi_in_get_current_api (RtMidiPtr device);

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal void rtmidi_in_set_callback (RtMidiInPtr device, RtMidiCCallback callback, IntPtr userData);

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal void rtmidi_in_cancel_callback (RtMidiInPtr device);

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal void rtmidi_in_ignore_types (RtMidiInPtr device, bool midiSysex, bool midiTime, bool midiSense);

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal double rtmidi_in_get_message (RtMidiInPtr device, /* unsigned char ** */out IntPtr message);

		/* RtMidiOut API */
		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal RtMidiOutPtr rtmidi_out_create_default ();

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal RtMidiOutPtr rtmidi_out_create (RtMidiApi api, string clientName);

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal void rtmidi_out_free (RtMidiOutPtr device);

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal RtMidiApi rtmidi_out_get_current_api (RtMidiPtr device);

		[DllImport (RtMidiLibrary, CallingConvention = CallingConvention.Cdecl)]
		static extern internal int rtmidi_out_send_message (RtMidiOutPtr device, byte [] message, int length);
	}
	
	// Wrapper classes

	public abstract class RtMidiDevice : IDisposable
	{
		// no idea when to use it...
		public static void Error (RtMidiErrorType errorType, string message)
		{
			RtMidi.rtmidi_error (errorType, message);
		}

		public static RtMidiApi [] GetAvailableApis ()
		{
			int enumSize = RtMidi.rtmidi_sizeof_rtmidi_api ();
			IntPtr ptr = IntPtr.Zero;
			int size = RtMidi.rtmidi_get_compiled_api (ref ptr);
			ptr = Marshal.AllocHGlobal (size * enumSize);
			RtMidi.rtmidi_get_compiled_api (ref ptr);
			RtMidiApi [] ret = new RtMidiApi [size];
			switch (enumSize) {
			case 1:
				byte [] bytes = new byte [size];
				Marshal.Copy (ptr, bytes, 0, bytes.Length);
				for (int i = 0; i < bytes.Length; i++)
					ret [i] = (RtMidiApi) bytes [i];
				break;
			case 2:
				short [] shorts = new short [size];
				Marshal.Copy (ptr, shorts, 0, shorts.Length);
				for (int i = 0; i < shorts.Length; i++)
					ret [i] = (RtMidiApi) shorts [i];
				break;
			case 4:
				int [] ints = new int [size];
				Marshal.Copy (ptr, ints, 0, ints.Length);
				for (int i = 0; i < ints.Length; i++)
					ret [i] = (RtMidiApi) ints [i];
				break;
			case 8:
				long [] longs = new long [size];
				Marshal.Copy (ptr, longs, 0, longs.Length);
				for (int i = 0; i < longs.Length; i++)
					ret [i] = (RtMidiApi) longs [i];
				break;
			default:
				throw new NotSupportedException ("sizeof RtMidiApi is unexpected: " + enumSize);
			}
			return ret;
		}

		RtMidiPtr handle;
		bool is_port_open;

		protected RtMidiDevice (RtMidiPtr handle)
		{
			this.handle = handle;
		}

		public RtMidiPtr Handle {
			get { return handle; }
		}

		public int PortCount {
			get { return (int) RtMidi.rtmidi_get_port_count (handle); }
		}

		public void Dispose ()
		{
			Close ();
		}

		public void Close ()
		{
			if (is_port_open) {
				is_port_open = false;
				RtMidi.rtmidi_close_port (handle);
			}
			ReleaseDevice ();
		}

		public string GetPortName (int portNumber)
		{
			return RtMidi.rtmidi_get_port_name (handle, (uint) portNumber);
		}

		public void OpenVirtualPort (string portName)
		{
			try {
				RtMidi.rtmidi_open_virtual_port (handle, portName);
			} finally {
				is_port_open = true;
			}
		}

		public void OpenPort (int portNumber, string portName)
		{
			try {
				RtMidi.rtmidi_open_port (handle, (uint) portNumber, portName);
			} finally {
				is_port_open = true;
			}
		}

		protected abstract void ReleaseDevice ();

		public abstract RtMidiApi CurrentApi { get; }
	}

	public class RtMidiInputDevice : RtMidiDevice
	{
		public RtMidiInputDevice ()
			: base (RtMidi.rtmidi_in_create_default ())
		{
		}

		public RtMidiInputDevice (RtMidiApi api, string clientName, int queueSizeLimit = 100)
			: base (RtMidi.rtmidi_in_create (api, clientName, (uint) queueSizeLimit))
		{
		}

		public override RtMidiApi CurrentApi {
			get { return RtMidi.rtmidi_in_get_current_api (Handle); }
		}

		protected override void ReleaseDevice ()
		{
			RtMidi.rtmidi_in_free (Handle);
		}

		public void SetCallback (RtMidiCCallback callback, IntPtr userData)
		{
			RtMidi.rtmidi_in_set_callback (Handle, callback, userData);
		}

		public void CancelCallback ()
		{
			RtMidi.rtmidi_in_cancel_callback (Handle);
		}

		public void SetIgnoredTypes (bool midiSysex, bool midiTime, bool midiSense)
		{
			RtMidi.rtmidi_in_ignore_types (Handle, midiSysex, midiTime, midiSense);
		}

		public byte [] GetMessage ()
		{
			IntPtr ptr;
			int size = (int) RtMidi.rtmidi_in_get_message (Handle, out ptr);
			byte [] buf = new byte [size];
			Marshal.Copy (ptr, buf, 0, size);
			return buf;
		}
	}

	public class RtMidiOutputDevice : RtMidiDevice
	{
		public RtMidiOutputDevice ()
			: base (RtMidi.rtmidi_out_create_default ())
		{
		}

		public RtMidiOutputDevice (RtMidiApi api, string clientName)
			: base (RtMidi.rtmidi_out_create (api, clientName))
		{
		}

		public override RtMidiApi CurrentApi {
			get { return RtMidi.rtmidi_out_get_current_api (Handle); }
		}

		protected override void ReleaseDevice ()
		{
			RtMidi.rtmidi_out_free (Handle);
		}

		public void SendMessage (byte [] message, int length)
		{
			if (message == null)
				throw new ArgumentNullException ("message");
			// While it could emit message parsing error, it still returns 0...!
			RtMidi.rtmidi_out_send_message (Handle, message, length);
		}
	}
	
	// Utility classes
	
	public static class MidiDeviceManager
	{
		static readonly RtMidiOutputDevice manager_output = new RtMidiOutputDevice ();
		static readonly RtMidiInputDevice manager_input = new RtMidiInputDevice ();
		
		// OK, it is not really a device count. But RTMIDI is designed to have bad names enough
		// to enumerate APIs as DEVICEs.
		public static int DeviceCount {
			get { return manager_input.PortCount + manager_output.PortCount; }
		}

		public static int DefaultInputDeviceID {
			get { return 0; }
		}

		public static int DefaultOutputDeviceID {
			get { return manager_input.PortCount; }
		}

		public static IEnumerable<MidiDeviceInfo> AllDevices {
			get {
				for (int i = 0; i < DeviceCount; i++)
					yield return GetDeviceInfo (i);
			}
		}

		public static MidiDeviceInfo GetDeviceInfo (int id)
		{
			return id < manager_input.PortCount ? new MidiDeviceInfo (manager_input, id, id, true) : new MidiDeviceInfo (manager_output, id, id - manager_input.PortCount, false);
		}

		public static RtMidiInputDevice OpenInput (int deviceID)
		{
			var dev = new RtMidiInputDevice ();
			dev.OpenPort (deviceID, GetDeviceInfo (deviceID).Name);
			return dev;
		}

		public static RtMidiOutputDevice OpenOutput (int deviceID)
		{
			var dev = new RtMidiOutputDevice ();
			dev.OpenPort (deviceID - manager_input.PortCount, GetDeviceInfo (deviceID).Name);
			return dev;
		}
	}

	public class MidiDeviceInfo
	{
		readonly RtMidiDevice manager;
		readonly int id;
		readonly int port;
		readonly bool is_input;

		internal MidiDeviceInfo (RtMidiDevice manager, int id, int port, bool isInput)
		{
			this.manager = manager;
			this.id = id;
			this.port = port;
			is_input = isInput;
		}

		public int ID {
			get { return id; }
		}

		public int Port {
			get { return port; }
		}

		public string Interface {
			get { return manager.CurrentApi.ToString (); }
		}

		public string Name {
			get { return manager.GetPortName (port); }
		}

		public bool IsInput { get { return is_input; } }

		public bool IsOutput { get { return !is_input; } }

		public override string ToString ()
		{
			return String.Format ("{0} - {1} ({2})", Interface, Name, IsInput ? (IsOutput ? "I/O" : "Input") : (IsOutput ? "Output" : "N/A"));
		}
	}
}
