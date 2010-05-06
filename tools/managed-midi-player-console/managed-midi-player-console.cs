using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Commons.Music.Midi;
using PortMidiSharp;
using Timer = System.Timers.Timer;

namespace Commons.Music.Midi.Player
{
	public class Driver
	{
		public static void Main (string [] args)
		{
			int outdev = MidiDeviceManager.DefaultOutputDeviceID;
			var files = new List<string> ();
			foreach (var arg in args) {
				if (arg.StartsWith ("--device:")) {
					if (!int.TryParse (arg.Substring (9), out outdev)) {
						Console.WriteLine ("Specify device ID: ");
						foreach (var dev in MidiDeviceManager.AllDevices)
							if (dev.IsOutput)
								Console.WriteLine ("{0}: {1}", dev.ID, dev.Name);
						return;
					}
				}
				else
					files.Add (arg);
			}
			var output = MidiDeviceManager.OpenOutput (outdev);

			foreach (var arg in files) {
				var parser = new SmfReader (File.OpenRead (arg));
				parser.Parse ();
				var player = new PortMidiPlayer (output, parser.Music);
				player.StartLoop ();
				player.PlayAsync ();
				Console.WriteLine ("empty line to quit, P to pause and resume");
				while (true) {
					string line = Console.ReadLine ();
					if (line == "P") {
						if (player.State == PlayerState.Playing)
							player.PauseAsync ();
						else
							player.PlayAsync ();
					}
					else if (line == "") {
						player.Dispose ();
						break;
					}
					else
						Console.WriteLine ("what do you mean by '{0}' ?", line);
				}
			}
		}
	}
}

