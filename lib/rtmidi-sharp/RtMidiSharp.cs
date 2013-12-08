using System;
using System.IO;
using System.Runtime.InteropServices;

using RtMidiPtr = System.IntPtr;
using RtMidiInPtr = System.IntPtr;
using RtMidiOutPtr = System.IntPtr;


namespace RtMidiSharp
{
	public enum RtMidiApi {
		Unspecified,
		MacOsxCore,
		LinuxAlsa,
		UnixJack,
		WindowsMultimediaMidi,
		WindowsKernelStreaming,
		RtMidiDummy,
	}

	public enum RtMidiErrorType {
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


	public delegate void RtMidiCallback (double timestamp, string message, IntPtr userData);

	public static class RtMidi
	{
		public const string RtMidiLibrary = "rtmidi_c";
		
		/* Utility API */
		[DllImport (RtMidiLibrary)]
		static extern internal int rtmidi_sizeof_rtmidi_api ();
		
		/* RtMidi API */
		[DllImport (RtMidiLibrary)]
		static extern internal int rtmidi_get_compiled_api (ref IntPtr/* RtMidiApi ** */ apis); // return length for NULL argument.
		[DllImport (RtMidiLibrary)]
		static extern internal void rtmidi_error (RtMidiErrorType type, string errorString);

		[DllImport (RtMidiLibrary)]
		static extern internal void rtmidi_open_port (RtMidiPtr device, uint portNumber, string portName);
		[DllImport (RtMidiLibrary)]
		static extern internal void rtmidi_open_virtual_port (RtMidiPtr device, string portName);
		[DllImport (RtMidiLibrary)]
		static extern internal void rtmidi_close_port (RtMidiPtr device);
		[DllImport (RtMidiLibrary)]
		static extern internal uint rtmidi_get_port_count (RtMidiPtr device);
		[DllImport (RtMidiLibrary)]
		static extern internal string rtmidi_get_port_name (RtMidiPtr device, uint portNumber);

		/* RtMidiIn API */
		[DllImport (RtMidiLibrary)]
		static extern internal RtMidiInPtr rtmidi_in_create_default ();
		[DllImport (RtMidiLibrary)]
		static extern internal RtMidiInPtr rtmidi_in_create (RtMidiApi api, string clientName, uint queueSizeLimit);
		[DllImport (RtMidiLibrary)]
		static extern internal void rtmidi_in_free (RtMidiInPtr device);
		[DllImport (RtMidiLibrary)]
		static extern internal RtMidiApi rtmidi_in_get_current_api (RtMidiPtr device);
		[DllImport (RtMidiLibrary)]
		static extern internal void rtmidi_in_set_callback (RtMidiInPtr device, RtMidiCallback callback, IntPtr userData);
		[DllImport (RtMidiLibrary)]
		static extern internal void rtmidi_in_cancel_callback (RtMidiInPtr device);
		[DllImport (RtMidiLibrary)]
		static extern internal void rtmidi_in_ignore_types (RtMidiInPtr device, bool midiSysex, bool midiTime, bool midiSense);
		[DllImport (RtMidiLibrary)]
		static extern internal double rtmidi_in_get_message (RtMidiInPtr device, /* unsigned char ** */out IntPtr message);

		/* RtMidiOut API */
		[DllImport (RtMidiLibrary)]
		static extern internal RtMidiOutPtr rtmidi_out_create_default ();
		[DllImport (RtMidiLibrary)]
		static extern internal RtMidiOutPtr rtmidi_out_create (RtMidiApi api, string clientName);
		[DllImport (RtMidiLibrary)]
		static extern internal void rtmidi_out_free (RtMidiOutPtr device);
		[DllImport (RtMidiLibrary)]
		static extern internal RtMidiApi rtmidi_out_get_current_api (RtMidiPtr device);
		[DllImport (RtMidiLibrary)]
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
			if (handle == IntPtr.Zero)
				throw new ArgumentException ("Non-null MIDI device handle is expected");
			this.handle = handle;
		}
		
		protected IntPtr Handle {
			get { return handle; }
		}
		
		public int PortCount {
			get { return (int) RtMidi.rtmidi_get_port_count (handle); }
		}
		
		public void Dispose ()
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
		
		public void SetCallback (RtMidiCallback callback, IntPtr userData)
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
		
		public void SendMessage (byte [] message)
		{
			if (message == null)
				throw new ArgumentNullException ("message");
			RtMidi.rtmidi_out_send_message (Handle, message, message.Length);
		}
	}
}
