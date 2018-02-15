using System;
using System.IO;
using System.Linq;
using Commons.Music.Midi;

namespace ManagedMidiRecorder
{
	class MainClass 
	{
		public static void Main (string [] args)
		{
			string outfile = args.FirstOrDefault ();
			Stream outStream = outfile != null ? File.OpenWrite (outfile) : null;

			var access = MidiAccessManager.Default;
			foreach (var i in access.Inputs)
				Console.WriteLine (i.Id + " : " + i.Name);
			if (!access.Inputs.Any()) {
				Console.WriteLine("No input device found.");
				return;
			}
			Console.WriteLine ("Using last one");
			var input = access.OpenInputAsync (access.Inputs.Last ().Id).Result;
			input.MessageReceived += (obj, e) => {
				Console.WriteLine ($"{e.Timestamp} {e.Start} {e.Length} {e.Data [0].ToString ("X")}");
				if (outStream != null)
	    				outStream.Write (e.Data, e.Start, e.Length);
			};
			Console.WriteLine ("Type [CR] to quit...");
			Console.ReadLine ();
			input.CloseAsync ();

			if (outStream != null)
				outStream.Close ();
		}
	}
}
