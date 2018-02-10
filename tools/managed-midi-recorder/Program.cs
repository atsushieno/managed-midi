using System;
using System.Linq;
using Commons.Music.Midi;

namespace ManagedMidiRecorder
{
	class MainClass 
	{
		public static void Main (string [] args)
		{
			var access = MidiAccessManager.Default;
			foreach (var i in access.Inputs)
				Console.WriteLine (i.Id + " : " + i.Name);
			var input = access.OpenInputAsync (access.Inputs.Last ().Id).Result;
			input.MessageReceived += (obj, e) => {
				Console.WriteLine ($"{e.Timestamp} {e.Start} {e.Length} {e.Data [0].ToString ("X")}");
			};
			Console.WriteLine ("Type [CR] to quit...");
			Console.ReadLine ();
			input.CloseAsync ();
		}
	}
}
