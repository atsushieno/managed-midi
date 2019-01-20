using System;
using System.Runtime.InteropServices;

namespace Commons.Music.Midi
{
	public partial class MidiAccessManager
	{
		partial void InitializeDefault ()
		{
			Default =
				Environment.OSVersion.Platform != PlatformID.Unix ? (IMidiAccess) new WinMM.WinMMMidiAccess () :
				IsRunningOnMac () ? (IMidiAccess) new CoreMidiApi.CoreMidiAccess () : new Alsa.AlsaMidiAccess ();
		}

		//From Managed.Windows.Forms/XplatUI
		[DllImport ("libc")]
		static extern int uname (IntPtr buf);

		static bool IsRunningOnMac ()
		{
			IntPtr buf = IntPtr.Zero;
			try {
				buf = Marshal.AllocHGlobal (8192);
				// This is a hacktastic way of getting sysname from uname ()
				if (uname (buf) == 0) {
					string os = Marshal.PtrToStringAnsi (buf);
					if (os == "Darwin")
						return true;
				}
			} catch {
			} finally {
				if (buf != IntPtr.Zero)
					Marshal.FreeHGlobal (buf);
			}
			return false;
		}
	}
}
