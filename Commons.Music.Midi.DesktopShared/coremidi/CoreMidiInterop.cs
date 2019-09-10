using System;
using System.Runtime.InteropServices;

using MIDIClientRef = System.IntPtr;
using MIDIPortRef = System.IntPtr;
using MIDIEndpointRef = System.IntPtr;
using MIDIDeviceRef = System.IntPtr;
using MIDIEntityRef = System.IntPtr;
using MIDIObjectRef = System.IntPtr;
using CFStringRef = System.IntPtr;
using CFDataRef = System.IntPtr;
using CFDictionaryRef = System.IntPtr;
using CFPropertyListRef = System.IntPtr;
using MIDINotificationPtr = System.IntPtr;
using MIDIPacketListPtr = System.IntPtr;
using MIDIPacketPtr = System.IntPtr;
using MIDISysexSendRequestPtr = System.IntPtr;
using ByteCount = System.Int32;
using MIDIObjectType = System.Int32;
using MIDITimeStamp = System.Int64;
using MIDIUniqueID = System.Int32;
using OSStatus = System.Int32;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Commons.Music.Midi.PortMidi;
using nint = System.Int64;
using ItemCount = System.Int64;

using CFAllocatorRef = System.IntPtr;
using CFStringEncoding = System.Int32;
using CFTypeRef = System.IntPtr;

namespace CoreMidi {

	public static class Midi
	{
		public static nint DeviceCount => CoreMidiInterop.MIDIGetNumberOfDevices ();

		public static MidiDevice GetDevice (nint d) => new MidiDevice (CoreMidiInterop.MIDIGetDevice (d));

		public static nint SourceCount => CoreMidiInterop.MIDIGetNumberOfSources ();

		public static nint DestinationCount => CoreMidiInterop.MIDIGetNumberOfDestinations ();

		internal static CFStringRef ToCFStringRef (string s)
		{
			return CoreFoundationInterop.CFStringCreateWithCString (IntPtr.Zero, s, CoreFoundationInterop.kCFStringEncodingUTF8);
		}
	}

	public class MidiException : Exception
	{
		public MidiException ()
			: this ("MIDI error")
		{
		}

		public MidiException (string message)
			: this(message, null)
		{
		}

		public MidiException (string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected MidiException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
		}
	}
	
	public enum MidiError {
		Ok = 0,
		InvalidClient = -10830,
		InvalidPort = -10831,
		WrongEndpointType = -10832,
		NoConnection = -10833,
		UnknownEndpoint = -10834,
		UnknownProperty = -10835,
		WrongPropertyType = -10836,
		NoCurrentSetup = -10837,
		MessageSendErr = -10838,
		ServerStartErr = -10839,
		SetupFormatErr = -10840,
		WrongThread = -10841,
		ObjectNotFound = -10842,
		IDNotUnique = -10843,
		NotPermitted = -10844
	}

	public class MidiDevice 
	{
		public MidiDevice (MIDIDeviceRef device)
		{
			this.device = device;
		}

		IntPtr device;

		public nint EntityCount => CoreMidiInterop.MIDIDeviceGetNumberOfEntities (device);

		public MidiEntity GetEntity (nint e)
		{
			return new MidiEntity (CoreMidiInterop.MIDIDeviceGetEntity (device, e));
		}
	}

	public class MidiEntity 
	{
		public MidiEntity (MIDIEntityRef entity)
		{
			this.entity = entity;
		}

		MIDIEntityRef entity;

		public nint Sources => CoreMidiInterop.MIDIEntityGetNumberOfSources (entity);

		public nint Destinations => CoreMidiInterop.MIDIEntityGetNumberOfDestinations (entity);
	}

	public class MidiEndpoint : IDisposable
	{

		public static MidiEndpoint GetSource (nint s) => new MidiEndpoint (CoreMidiInterop.MIDIGetSource (s), "Source" + s, false, null);

		public static MidiEndpoint GetDestination (nint d) => new MidiEndpoint (CoreMidiInterop.MIDIGetDestination (d), "Destination" + d, false, null);

		public MidiEndpoint (MIDIEndpointRef endpoint, string endpointName, bool shouldDispose, ReadDispatcher dispatcher)
		{
			Handle = endpoint;
			should_dispose = shouldDispose;
			EndpointName = endpointName;
			this.dispatcher = dispatcher;
		}

		bool should_dispose;
		ReadDispatcher dispatcher;

		public MIDIEndpointRef Handle { get; private set; }

		public string EndpointName { get; private set; }

		public string Name
		{
			get { return GetStringProp (CoreMidiIntropWorkaround.kMIDIPropertyName); }
			set { SetStringProp (CoreMidiIntropWorkaround.kMIDIPropertyName, value); }
		}

		public string Manufacturer
		{
			get { return GetStringProp (CoreMidiIntropWorkaround.kMIDIPropertyManufacturer); }
			set { SetStringProp (CoreMidiIntropWorkaround.kMIDIPropertyManufacturer, value); }
		}

		public string DisplayName
		{
			get { return GetStringProp (CoreMidiIntropWorkaround.kMIDIPropertyDisplayName); }
			set { SetStringProp (CoreMidiIntropWorkaround.kMIDIPropertyDisplayName, value); }
		}

		public int DriverVersion
		{
			get { return GetIntegerProp (CoreMidiIntropWorkaround.kMIDIPropertyDriverVersion); }
			set { SetIntegerProp (CoreMidiIntropWorkaround.kMIDIPropertyDriverVersion, value); }
		}

		void SetStringProp (IntPtr id, string value)
		{
			if (id == IntPtr.Zero)
				return;
			CFStringRef str;
			CoreMidiInterop.MIDIObjectSetStringProperty (Handle, id, Midi.ToCFStringRef (value));
		}

		String GetStringProp (IntPtr id)
		{
			if (id == IntPtr.Zero)
				return null;

			CFStringRef str;
			CoreMidiInterop.MIDIObjectGetStringProperty (Handle, id, out str);
			var cstr = CoreFoundationInterop.CFStringGetCStringPtr (str, CoreFoundationInterop.kCFStringEncodingUTF8);
			unsafe {
				if (cstr == IntPtr.Zero)
					return null;
				byte* p = (byte*) cstr;
				int count = 0;
				for (byte* i = p; *i != 0; i++)
					count++;
				return System.Text.Encoding.UTF8.GetString ((byte*)cstr, count);
			}
		}

		void SetIntegerProp (IntPtr id, int value)
		{
			CoreMidiInterop.MIDIObjectSetIntegerProperty (Handle, id, value);
		}

		int GetIntegerProp(IntPtr id)
		{
			int ret;
			CoreMidiInterop.MIDIObjectGetIntegerProperty(Handle, id, out ret);
			return ret;
		}

		public void Dispose()
		{
			if (should_dispose)
			{
				dispatcher?.Dispose();
				dispatcher = null;
				should_dispose = false;
			}
		}
	}

	public class MidiPort : IDisposable {
		public MidiPort (MIDIPortRef port, bool shouldDispose, ReadDispatcher dispatcher)
		{
			Handle = port;
			should_dispose = shouldDispose;
			this.dispatcher = dispatcher;
		}
		bool should_dispose;
		ReadDispatcher dispatcher;

		public MIDIPortRef Handle { get; private set; }

		public void CallMessageReceived (MIDIPacketListPtr pktlist, IntPtr readProcRefCon, IntPtr srcConnRefCon)
		{
			if (MessageReceived == null)
				return;
			var packets = new List<MidiPacket> ();
			var list = Marshal.PtrToStructure<MidiPacketListNative> (pktlist);
			var p = pktlist + 4;
			for (int i = 0; i < list.NumPackets; i++) {
				var packet = Marshal.PtrToStructure<MIDIPacketNative> (p);
				packets.Add (new MidiPacket (packet.TimeStamp, packet.Length, p + 10));
				p = CoreMidiInterop.MIDIPacketNext (p);
			}
			MessageReceived (this, new MidiPacketsEventArgs { Packets = packets.ToArray () });
		}

		public Action<object, MidiPacketsEventArgs> MessageReceived { get; internal set; }

		public void Dispose ()
		{
			if (list != IntPtr.Zero) {
				Marshal.FreeHGlobal (list);
				list = IntPtr.Zero;
			}

			if (should_dispose) {
				CoreMidiInterop.MIDIPortDispose (Handle);
				dispatcher?.Dispose();
				dispatcher = null;
				should_dispose = false;
			}
		}

		public void Disconnect (MidiEndpoint endpoint) => CoreMidiInterop.MIDIPortDisconnectSource (Handle, endpoint.Handle);

		public void ConnectSource (MidiEndpoint endpoint) => CoreMidiInterop.MIDIPortConnectSource (Handle, endpoint.Handle, IntPtr.Zero);

		int buf_size = 1024;
		IntPtr list;

		public void Send (MidiEndpoint endpoint, MidiPacket [] arr)
		{
			
			var msize = Marshal.SizeOf<MIDIPacketNative> ();
			var size = arr.Select (a => msize + a.Length).Sum ();
			if (list == IntPtr.Zero || size > buf_size) {
				if (list != IntPtr.Zero)
					Marshal.FreeHGlobal (list);
				list = Marshal.AllocHGlobal (buf_size);
			}

			var p = CoreMidiInterop.MIDIPacketListInit (list);
			foreach (var item in arr)
				p = CoreMidiInterop.MIDIPacketListAdd (list, 1024, p, item.TimeStamp, item.Length, item.Bytes);
			CoreMidiInterop.MIDISend (Handle, endpoint.Handle, list);
		}
	}

	public class MidiPacket
	{
		public MidiPacket (long timestamp, ushort length, IntPtr bytes)
		{
			this.TimeStamp = timestamp;
			this.Length = length;
			this.Bytes = bytes;
		}

		public int Length { get; private set; }

		public IntPtr Bytes { get; private set;  }
		public long TimeStamp { get; internal set; }
	}

	public class ReadDispatcher : IDisposable
	{
		private static List<CoreMidiInterop.MIDIReadProc> read_procs = new List<CoreMidiInterop.MIDIReadProc>();

		public MidiPort Port { get; set; }
		internal CoreMidiInterop.MIDIReadProc DispatchProc;

		public ReadDispatcher()
		{
			DispatchProc = dispatchRead;

			lock (read_procs)
				read_procs.Add(DispatchProc);
		}

		private void dispatchRead (MIDIPacketListPtr pktlist, IntPtr readProcRefCon, IntPtr srcConnRefCon)
		{
			Port.CallMessageReceived (pktlist, readProcRefCon, srcConnRefCon);
		}

		public void Dispose()
		{
			lock (read_procs)
				read_procs.Remove(DispatchProc);
		}
	}

	public class MidiClient : IDisposable
	{
		public MidiClient (string name)
		{
			IntPtr h;
			name_string = Midi.ToCFStringRef (name);
			int ret = CoreMidiInterop.MIDIClientCreate (name_string, OnNotify, IntPtr.Zero, out h);
			if (ret != 0)
				throw new MidiException ($"Failed to create MIDI client for {name}: error code {ret}");
			Handle = h;
		}

		CFStringRef name_string;

		void OnNotify (IntPtr message, IntPtr refCon)
		{
			throw new NotImplementedException ();
		}

		public MIDIClientRef Handle { get; private set; }

		public MidiPort CreateInputPort (string name)
		{
			MIDIPortRef port;
			var d = new ReadDispatcher ();
			CoreMidiInterop.MIDIInputPortCreate(Handle, Midi.ToCFStringRef (name), d.DispatchProc, IntPtr.Zero, out port);
			d.Port = new MidiPort (port, true, d);
			return d.Port;
		}

		public MidiPort CreateOutputPort (string name)
		{
			MIDIPortRef port;
			CoreMidiInterop.MIDIOutputPortCreate (Handle, Midi.ToCFStringRef (name), out port);
			return new MidiPort (port, true, null);
		}

		public MidiEndpoint CreateVirtualSource (string name, out MidiError statusCode)
		{
			IntPtr ptr;
			statusCode = (MidiError) CoreMidiInterop.MIDISourceCreate (Handle, Midi.ToCFStringRef (name), out ptr);
			return statusCode == MidiError.Ok ? new MidiEndpoint (ptr, name, true, null) : null;
		}

		public MidiEndpoint CreateVirtualDestination (string name, out MidiError statusCode)
		{
			IntPtr ptr;
			var d = new ReadDispatcher ();
			statusCode = (MidiError) CoreMidiInterop.MIDIDestinationCreate (Handle, Midi.ToCFStringRef (name),
				d.DispatchProc, IntPtr.Zero, out ptr);
			return statusCode == MidiError.Ok ? new MidiEndpoint (ptr, name, true, d) : null;
		}

		public void Dispose ()
		{
			CoreMidiInterop.MIDIClientDispose (Handle);
			CoreFoundationInterop.CFRelease (name_string);

		}
	}

	public class MidiPacketsEventArgs : EventArgs
	{
		public MidiPacket[] Packets { get; internal set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	internal struct MidiPacketListNative {
		public uint NumPackets;
		public IntPtr Packet; // to MIDPacketNative
	}

	[StructLayout (LayoutKind.Sequential)]
	internal struct MIDIPacketNative {
		public MIDITimeStamp TimeStamp;
		public ushort Length;
		//[MarshalAs (UnmanagedType.LPArray, SizeConst = 256)]
		public IntPtr Data;
	}

	// I have no idea why it doesn't work if all these pieces go into CoreMidiInterop class...
	internal class CoreMidiIntropWorkaround {
		//const string LibraryName = "/System/Library/Frameworks/CoreMIDI.framework/Resources/BridgeSupport/CoreMIDI.dylib";
		const string LibraryName = "/System/Library/Frameworks/CoreMIDI.framework/Versions/A/CoreMIDI";

		static CoreMidiIntropWorkaround ()
		{
			var dl = dlopen (LibraryName);
			try {
				IntPtr ptr;
				ptr = dlsym (dl, nameof (kMIDIPropertyName));
				kMIDIPropertyName = ptr != IntPtr.Zero ? Marshal.ReadIntPtr (ptr) : ptr;
				ptr = dlsym (dl, nameof (kMIDIPropertyDisplayName));
				kMIDIPropertyDisplayName = ptr != IntPtr.Zero ? Marshal.ReadIntPtr (ptr) : ptr;
				ptr = dlsym (dl, nameof (kMIDIPropertyManufacturer));
				kMIDIPropertyManufacturer = ptr != IntPtr.Zero ? Marshal.ReadIntPtr (ptr) : ptr;
				ptr = dlsym (dl, nameof (kMIDIPropertyDriverVersion));
				kMIDIPropertyDriverVersion = ptr != IntPtr.Zero ? Marshal.ReadIntPtr (ptr) : ptr;
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
			dlclose (dl);
		}

		public static readonly IntPtr kMIDIPropertyName;
		public static readonly IntPtr kMIDIPropertyDisplayName;
		public static readonly IntPtr kMIDIPropertyManufacturer;
		public static readonly IntPtr kMIDIPropertyDriverVersion;

		[DllImport ("/usr/lib/libSystem.dylib")]
		static extern IntPtr dlopen (string filename);
		[DllImport ("/usr/lib/libSystem.dylib")]
		static extern IntPtr dlsym (IntPtr dl, string symbol);
		[DllImport ("/usr/lib/libSystem.dylib")]
		static extern IntPtr dlclose (IntPtr dl);
		[DllImport ("/usr/lib/libSystem.dylib")]
		static extern int dlerror ();
	}

	internal class CoreMidiInterop
	{
		const string LibraryName = "/System/Library/Frameworks/CoreMIDI.framework/Resources/BridgeSupport/CoreMIDI.dylib";

		static CoreMidiInterop ()
		{
		}

		public delegate void MIDICompletionProc (MIDISysexSendRequestPtr request);
		public delegate void MIDINotifyProc (MIDINotificationPtr message, IntPtr refCon);
		public delegate void MIDIReadProc (MIDIPacketListPtr pktlist, IntPtr readProcRefCon, IntPtr srcConnRefCon);

		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIInputPortCreate (MIDIClientRef client, CFStringRef portName, MIDIReadProc readProc, IntPtr refCon, out MIDIPortRef outPort);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIOutputPortCreate (MIDIClientRef client, CFStringRef portName, out MIDIPortRef outPort);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIPortConnectSource (MIDIPortRef port, MIDIEndpointRef source, IntPtr connRefCon);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIPortDisconnectSource (MIDIPortRef port, MIDIEndpointRef source);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIPortDispose (MIDIPortRef port);

		[DllImport (LibraryName)]
		internal static unsafe extern MIDIPacketPtr MIDIPacketListAdd (MIDIPacketListPtr pktlist, ByteCount listSize, MIDIPacketPtr curPacket, MIDITimeStamp time, ByteCount nData, IntPtr data);
		[DllImport (LibraryName)]
		internal static unsafe extern MIDIPacketPtr MIDIPacketListInit (MIDIPacketListPtr pktlist);
		[DllImport (LibraryName)]
		internal static unsafe extern MIDIPacketPtr MIDIPacketNext (MIDIPacketPtr pkt);

		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIObjectFindByUniqueID (MIDIUniqueID inUniqueID, MIDIObjectRef* outObject, out MIDIObjectType outObjectType);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIObjectGetDataProperty (MIDIObjectRef obj, CFStringRef propertyID, out CFDataRef outData);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIObjectGetDictionaryProperty (MIDIObjectRef obj, CFStringRef propertyID, out CFDictionaryRef outDict);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIObjectGetIntegerProperty (MIDIObjectRef obj, CFStringRef propertyID, out Int32 outValue);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIObjectGetProperties (MIDIObjectRef obj, out CFPropertyListRef outProperties, Boolean deep);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIObjectGetStringProperty (MIDIObjectRef obj, CFStringRef propertyID, out CFStringRef str);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIObjectRemoveProperty (MIDIObjectRef obj, CFStringRef propertyID);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIObjectSetDataProperty (MIDIObjectRef obj, CFStringRef propertyID, CFDataRef data);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIObjectSetDictionaryProperty (MIDIObjectRef obj, CFStringRef propertyID, CFDictionaryRef dict);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIObjectSetIntegerProperty (MIDIObjectRef obj, CFStringRef propertyID, Int32 value);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIObjectSetStringProperty (MIDIObjectRef obj, CFStringRef propertyID, CFStringRef str);

		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIFlushOutput (MIDIEndpointRef dest);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIReceived (MIDIEndpointRef src, MIDIPacketListPtr pktlist);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIRestart ();
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDISend (MIDIPortRef port, MIDIEndpointRef dest, MIDIPacketListPtr pktlist);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDISendSysex (MIDISysexSendRequestPtr request);

		[DllImport (LibraryName)]
		internal static unsafe extern MIDIDeviceRef MIDIGetExternalDevice (ItemCount deviceIndex0);
		[DllImport (LibraryName)]
		internal static unsafe extern ItemCount MIDIGetNumberOfExternalDevices ();
		[DllImport (LibraryName)]
		internal static unsafe extern MIDIEndpointRef MIDIEntityGetDestination (MIDIEntityRef entity, ItemCount destIndex0);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIEntityGetDevice (MIDIEntityRef inEntity, out MIDIDeviceRef outDevice);
		[DllImport (LibraryName)]
		internal static unsafe extern ItemCount MIDIEntityGetNumberOfDestinations (MIDIEntityRef entity);
		[DllImport (LibraryName)]
		internal static unsafe extern ItemCount MIDIEntityGetNumberOfSources (MIDIEntityRef entity);
		[DllImport (LibraryName)]
		internal static unsafe extern MIDIEndpointRef MIDIEntityGetSource (MIDIEntityRef entity, ItemCount sourceIndex0);

		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIDestinationCreate (MIDIClientRef client, CFStringRef name, MIDIReadProc readProc, IntPtr refCon, out MIDIEndpointRef outDest);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIEndpointDispose (MIDIEndpointRef endpt);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIEndpointGetEntity (MIDIEndpointRef inEndpoint, out MIDIEntityRef outEntity);
		[DllImport (LibraryName)]
		internal static unsafe extern MIDIEndpointRef MIDIGetDestination (ItemCount destIndex0);
		[DllImport (LibraryName)]
		internal static unsafe extern ItemCount MIDIGetNumberOfDestinations ();
		[DllImport (LibraryName)]
		internal static unsafe extern ItemCount MIDIGetNumberOfSources ();
		[DllImport (LibraryName)]
		internal static unsafe extern MIDIEndpointRef MIDIGetSource (ItemCount sourceIndex0);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDISourceCreate (MIDIClientRef client, CFStringRef name, out MIDIEndpointRef outSrc);

		[DllImport (LibraryName)]
		internal static unsafe extern MIDIEntityRef MIDIDeviceGetEntity (MIDIDeviceRef device, ItemCount entityIndex0);
		[DllImport (LibraryName)]
		internal static unsafe extern ItemCount MIDIDeviceGetNumberOfEntities (MIDIDeviceRef device);
		[DllImport (LibraryName)]
		internal static unsafe extern MIDIDeviceRef MIDIGetDevice (ItemCount deviceIndex0);
		[DllImport (LibraryName)]
		internal static unsafe extern ItemCount MIDIGetNumberOfDevices ();

		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIClientCreate (CFStringRef name, MIDINotifyProc notifyProc, IntPtr notifyRefCon, out MIDIClientRef outClient);
		[DllImport (LibraryName)]
		internal static unsafe extern OSStatus MIDIClientDispose (MIDIClientRef client);
	}
}
