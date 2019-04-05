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
			string port = null;
			string outfile = null;
			foreach (var arg in args) {
				if (arg.StartsWith ("--port:"))
					port = arg.Substring ("--port:".Length);
				else
					outfile = arg;
			}
			Stream outStream = outfile != null ? File.OpenWrite (outfile) : null;

			var access = MidiAccessManager.Default;
			foreach (var i in access.Inputs)
				Console.WriteLine (i.Id + " : " + i.Name);
			if (!access.Inputs.Any()) {
				Console.WriteLine("No input device found.");
				return;
			}
			var iport = access.Inputs.FirstOrDefault (i => i.Id == port) ?? access.Inputs.Last ();
			var input = access.OpenInputAsync (iport.Id).Result;
			Console.WriteLine ("Using " + iport.Id);
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
