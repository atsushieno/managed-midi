using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
--verbose	verbose MIDI message outputs to console.
");
			Console.WriteLine ("List of MIDI output device IDs: ");
			foreach (var dev in MidiAccessManager.Default.Outputs)
				Console.WriteLine ("\t{0}: {1}", dev.Id, dev.Name);
		}

		public static void Main (string [] args)
		{
			var api = MidiAccessManager.Default;
			var output = api.Outputs.LastOrDefault ();
			var files = new List<string> ();
			bool diagnostic = false;
			if (args.Length == 0) {
				ShowHelp ();
				return;
			}
			foreach (var arg in args) {
				if (arg == "--help") {
					ShowHelp ();
					return;
				}
				else if (arg == "--verbose")
					diagnostic = true;
				else if (arg.StartsWith ("--device:", StringComparison.Ordinal)) {
					output = api.Outputs.FirstOrDefault (o => o.Id == arg.Substring (9));
					if (output == null) {
						ShowHelp ();
						Console.WriteLine ();
						Console.WriteLine ("Invalid MIDI output device ID.");
						return;
					}
				}
				else
					files.Add (arg);
			}

			var wh = new ManualResetEvent (false);
			bool loop = true;
			
			foreach (var arg in files) {
				var parser = new SmfReader ();
				parser.Read (File.OpenRead (arg));
				var player = new MidiPlayer (parser.Music, api.OpenOutputAsync (output.Id).Result);
				DateTimeOffset start = DateTimeOffset.Now;
				if (diagnostic)
					player.EventReceived += e => {
						string type = null;
						switch (e.EventType) {
						case SmfEvent.NoteOn: type = "NOn"; break;
						case SmfEvent.NoteOff: type = "NOff"; break;
						case SmfEvent.PAf: type = "PAf"; break;
						case SmfEvent.CC: type = "CC"; break;
						case SmfEvent.Program: type = "@"; break;
						case SmfEvent.CAf: type = "CAf"; break;
						case SmfEvent.Pitch: type = "P"; break;
						case SmfEvent.SysEx1: type = "SysEX"; break;
						case SmfEvent.SysEx2: type = "SysEX2"; break;
						case SmfEvent.Meta: type = "META"; break;
						}
						Console.WriteLine ("{0:06} {1:D02} {2} {3}", (DateTimeOffset.Now - start).TotalMilliseconds, e.Channel, type, e);
					};
				player.Finished += delegate {
					loop = false;
					wh.Set ();
				};
				
				new Task (() => {
					Console.WriteLine ("empty line to quit, P to pause and resume");
					while (loop) {
						string line = Console.ReadLine ();
						if (line == "P") {
							if (player.State == PlayerState.Playing)
								player.PauseAsync ();
							else
								player.PlayAsync ();
						}
						else if (line == "") {
							loop = false;
							wh.Set ();
							player.Dispose ();
							break;
						}
						else
							Console.WriteLine ("what do you mean by '{0}' ?", line);
					}
				}).Start ();

				//player.StartLoop ();
				player.PlayAsync ();
				while (loop) {
					wh.WaitOne ();
				}
			}
		}
	}
}

