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
--device:x	specify MIDI output device by ID.
--provider:x	specify custome MIDI access manager type
                (assembly qualified name).
--verbose	output verbose MIDI messages to the console.
");
			Console.WriteLine ("List of MIDI output device IDs: ");
			foreach (var dev in MidiAccessManager.Default.Outputs)
				Console.WriteLine ("\t{0}: {1}", dev.Id, dev.Name);
		}

		public static int Main (string [] args)
		{
			var apiProviderSpec = args.FirstOrDefault (a => a.StartsWith ("--provider:", StringComparison.Ordinal));
			Type apiType = null;
			if (apiProviderSpec != null) {
				apiType = Type.GetType (apiProviderSpec.Substring ("--provider:".Length));
				if (apiType == null) {
					ShowHelp ();
					Console.Error.WriteLine ();
					Console.Error.WriteLine (apiProviderSpec + " didn't work.");
					Console.Error.WriteLine ();
					return -1;
				}
				Console.Error.WriteLine ("Using MidiAccess '{0}'", apiType.AssemblyQualifiedName);
			}
			var api = apiProviderSpec != null ?
				(IMidiAccess) Activator.CreateInstance (apiType) :
				MidiAccessManager.Default;
			var output = api.Outputs.LastOrDefault ();
			var files = new List<string> ();
			bool diagnostic = false;
			foreach (var arg in args) {
				if (arg == apiProviderSpec)
					continue;
				if (arg == "--help") {
					ShowHelp ();
					return 0;
				}
				else if (arg == "--verbose")
					diagnostic = true;
				else if (arg.StartsWith ("--device:", StringComparison.Ordinal)) {
					output = api.Outputs.FirstOrDefault (o => o.Id == arg.Substring (9));
					if (output == null) {
						ShowHelp ();
						Console.WriteLine ();
						Console.WriteLine ("Invalid MIDI output device ID.");
						Console.Error.WriteLine ();
						return -2;
					}
				}
				else
					files.Add (arg);
			}
			if (!files.Any ()) {
				ShowHelp ();
				return 0;
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
						case MidiEvent.NoteOn: type = "NOn"; break;
						case MidiEvent.NoteOff: type = "NOff"; break;
						case MidiEvent.PAf: type = "PAf"; break;
						case MidiEvent.CC: type = "CC"; break;
						case MidiEvent.Program: type = "@"; break;
						case MidiEvent.CAf: type = "CAf"; break;
						case MidiEvent.Pitch: type = "P"; break;
						case MidiEvent.SysEx1: type = "SysEX"; break;
						case MidiEvent.SysEx2: type = "SysEX2"; break;
						case MidiEvent.Meta: type = "META"; break;
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
				player.PauseAsync ();
			}
			return 0;
		}
	}
}

