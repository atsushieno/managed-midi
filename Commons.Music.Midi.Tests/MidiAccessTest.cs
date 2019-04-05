using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;

namespace Commons.Music.Midi.Tests
{
	[TestFixture]
	public class MidiAccessTest
	{
		[Test]
		public void EmptySimpleOutput ()
		{
			var api = MidiAccessManager.Empty;
			Assert.AreEqual (1, api.Outputs.Count (), "output count");
			Assert.AreEqual (1, api.Inputs.Count (), "input count");

			var output = api.OpenOutputAsync (api.Outputs.First ().Id).Result;
			output.Send (new byte [3] { MidiEvent.NoteOn, 0, 0 }, 0, 0, 0);
			output.Send (new byte [3] { MidiEvent.NoteOff, 0, 0 }, 0, 0, 0);
			output.CloseAsync ().Wait ();
		}
		[Test]
		public void DefaultEnumerateIO ()
		{
			var api = MidiAccessManager.Default;
			api.Outputs.Count ();
			api.Inputs.Count ();
		}

		[Test]
		public void DefaultSimpleInput ()
		{
			var api = MidiAccessManager.Default;

			var dev = api.Inputs.FirstOrDefault (d => !d.Name.Contains ("Through"));
			if (dev == null)
				return;
			
			var input = api.OpenInputAsync (dev.Id).Result;
			var wait = new ManualResetEvent (false);
			byte [] data = null;
			input.MessageReceived += (o, e) => {
				data = new byte [e.Length];
				Array.Copy (e.Data, e.Start, data, 0, e.Length);
				wait.Set ();
			};
			Console.WriteLine ("Send some key on message through in one minute... " + dev.Name);
			wait.WaitOne (60000);
			input.CloseAsync ().Wait ();
			Assert.IsNotNull (data);
		}

		[Test]
		public void MidiPortCreatorExtension ()
		{
			var a2 = MidiAccessManager.Default as IMidiAccess2;
			if (a2 == null) {
				Assert.Warn ("not testable");
				return; // not testable
			}
			var pc = a2.ExtensionManager.GetInstance<MidiPortCreatorExtension> ();
			var sender = pc.CreateVirtualInputSender (new MidiPortCreatorExtension.PortCreatorContext ());
			sender.Send (new byte[] {0x90, 0x60, 0x70}, 0, 3, 0);
			sender.CloseAsync ();
		}
	}
}

