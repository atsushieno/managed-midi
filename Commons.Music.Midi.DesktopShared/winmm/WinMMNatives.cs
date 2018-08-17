using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Commons.Music.Midi.WinMM
{
	[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	struct MidiInCaps
	{
		public short Mid;
		public short Pid;
		public int DriverVersion;
		[MarshalAs (UnmanagedType.ByValTStr, SizeConst = WinMMNatives.MaxPNameLen)]
		public string Name;
		public int Support;
	}

	[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	struct MidiOutCaps
	{
		public short Mid;
		public short Pid;
		public int DriverVersion;
		[MarshalAs (UnmanagedType.ByValTStr, SizeConst = WinMMNatives.MaxPNameLen)]
		public string Name;
		public short Technology;
		public short Voices;
		public short Notes;
		public short ChannelMask;
		public int Support;
	}

	[StructLayout (LayoutKind.Sequential)]
	struct MidiHdr
	{
		public IntPtr Data;
		public int BufferLength;
		public int BytesRecorded;
		public IntPtr User;
		public int Flags;
		public IntPtr Next; // of MidiHdr
		public IntPtr Reserved;
		public int Offset;
		[MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
		private int[] reservedArray;
	}

	[Flags]
	public enum MidiInOpenFlags
	{
		Null = 0,
		Window = 0x10000,
		Task = 0x20000,
		Function = 0x30000,
		MidiIoStatus = 0x00020,
	}

	[Flags]
	public enum MidiOutOpenFlags
	{
		Null,
		Function,
		Thread,
		Window,
		Event,
	}

	public enum MidiInMessage : uint
	{
		Open = 0x3C1,
		Close = 0x3C2,
		Data = 0x3C3,
		LongData = 0x3C4,
		Error = 0x3C5,
		LongError = 0x3C6,
		MoreData = 0x3CC
	}

	public delegate void MidiInProc (IntPtr midiIn, MidiInMessage msg, IntPtr instance, IntPtr param1, IntPtr param2);
	public delegate void MidiOutProc (IntPtr midiOut, uint msg, IntPtr instance, IntPtr param1, IntPtr param2);

	public static class WinMMNatives
	{
		public const string LibraryName = "winmm";
		public const int MaxPNameLen = 32;

		[DllImport (LibraryName)]
		internal static extern int midiInGetNumDevs ();

		[DllImport (LibraryName)]
		internal static extern int midiOutGetNumDevs ();

		[DllImport (LibraryName)]
		internal static extern int midiInGetDevCaps (UIntPtr uDeviceID, out MidiInCaps midiInCaps, uint sizeOfMidiInCaps);

		[DllImport (LibraryName)]
		internal static extern int midiOutGetDevCaps (UIntPtr uDeviceID, out MidiOutCaps midiOutCaps, uint sizeOfMidiOutCaps);

		[DllImport (LibraryName)]
		internal static extern int midiInOpen (out IntPtr midiIn, uint deviceID, MidiInProc callback, IntPtr callbackInstance, MidiInOpenFlags flags);

		[DllImport (LibraryName)]
		internal static extern int midiInPrepareHeader (IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

		[DllImport (LibraryName)]
		internal static extern int midiInUnprepareHeader (IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

		[DllImport (LibraryName)]
		internal static extern int midiInAddBuffer (IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

		[DllImport (LibraryName)]
		internal static extern int midiOutOpen (out IntPtr midiIn, uint deviceID, MidiOutProc callback, IntPtr callbackInstance, MidiOutOpenFlags flags);

		[DllImport (LibraryName)]
		internal static extern int midiInStart (IntPtr midiIn);

		[DllImport (LibraryName)]
		internal static extern int midiInStop (IntPtr midiIn);

		[DllImport (LibraryName)]
		internal static extern int midiInClose (IntPtr midiIn);

		[DllImport (LibraryName)]
		internal static extern int midiOutClose (IntPtr midiIn);

		[DllImport (LibraryName)]
		internal static extern int midiOutMessage (IntPtr handle, uint msg, ref int dw1, ref int dw2);

		[DllImport (LibraryName)]
		internal static extern int midiOutShortMsg (IntPtr handle, uint msg);

		[DllImport (LibraryName)]
		internal static extern int midiOutLongMsg (IntPtr handle, IntPtr midiOutHdr, int midiOutHdrSize);

		[DllImport (LibraryName)]
		internal static extern int midiOutPrepareHeader (IntPtr handle, IntPtr midiOutHdr, int midiOutHdrSize);

		[DllImport (LibraryName)]
		internal static extern int midiOutUnprepareHeader (IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

		[DllImport (LibraryName)]
		internal static extern int midiInReset (IntPtr handle);

		[DllImport (LibraryName)]
		internal static extern int midiOutGetErrorText (int mmrError, StringBuilder message, int sizeOfMessage);

		[DllImport (LibraryName)]
		internal static extern int midiInGetErrorText (int mmrError, StringBuilder message, int sizeOfMessage);

		internal static string GetMidiOutErrorText (int code, int maxLength = 128)
		{
			StringBuilder errorMsg = new StringBuilder (maxLength);

			if (midiOutGetErrorText (code, errorMsg, maxLength) == 0) {
				return errorMsg.ToString ();
			}

			return "Unknown winmm midi output error";
		}

		internal static string GetMidiInErrorText (int code, int maxLength = 128)
		{
			StringBuilder errorMsg = new StringBuilder (maxLength);

			if (midiInGetErrorText (code, errorMsg, maxLength) == 0) {
				return errorMsg.ToString ();
			}

			return "Unknown winmm midi input error";
		}
	}
}
