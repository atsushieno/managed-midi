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
		static void ShowHelp ()
		{
			Console.WriteLine (@"
managed-midi-player-console [options] SMF-files(*.mid)

Options:
--help		show this help.
--device:x	specifies MIDI output device by ID.
");
			Console.WriteLine ("List of MIDI output device IDs: ");
			foreach (var dev in MidiDeviceManager.AllDevices)
				if (dev.IsOutput)
					Console.WriteLine ("\t{0}: {1}", dev.ID, dev.Name);
		}

		public static void Main (string [] args)
		{
			int outdev = MidiDeviceManager.DefaultOutputDeviceID;
			var files = new List<string> ();
			if (args.Length == 0) {
				ShowHelp ();
				return;
			}
			foreach (var arg in args) {
				if (arg == "--help") {
					ShowHelp ();
					return;
				}
				if (arg.StartsWith ("--device:")) {
					if (!int.TryParse (arg.Substring (9), out outdev)) {
						ShowHelp ();
						Console.WriteLine ();
						Console.WriteLine ("Invalid MIDI output device ID.");
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

