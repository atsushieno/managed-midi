// It is a utility to import module definition files for Domino MIDI sequencer: http://takabosoft.com/domino 
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Commons.Music.Midi
{
	static class XLinqExtensions
	{
		public static string Attr (this XElement el, string localName)
		{
			return (string) el.Attribute (XName.Get (localName));
		}

		public static int AttrAsInt (this XElement el, string localName)
		{
			return (int) el.Attribute (XName.Get (localName));
		}
	}

	public class DominoModuleXmlImporter
	{
		public static void Main (string [] args)
		{
			var imp = new DominoModuleXmlImporter ();
			foreach (string arg in args)
				using (var fs = File.OpenWrite (Path.ChangeExtension (arg, "midimod")))
					imp.Import (arg).Save (fs);
		}

		public MidiModuleDefinition Import (string file)
		{
			var doc = XDocument.Load (file);
			var mod = doc.Element ("ModuleData");
			var mdd = new MidiModuleDefinition () { Name = mod.Attr ("Name") };
			LoadMaps (mod, "InstrumentList", mdd.Instrument.Maps);
			LoadMaps (mod, "DrumSetList", mdd.Instrument.DrumMaps);
			return mdd;
		}

		void LoadMaps (XElement mod, string name, IList<MidiInstrumentMap> maps)
		{
			if (maps == null)
				throw new ArgumentException ("null maps");
			foreach (var ilist in mod.Elements (name))
				foreach (var map in ilist.Elements ("Map")) {
					var mad = new MidiInstrumentMap () { Name = map.Attr ("Name") };
					maps.Add (mad);
					foreach (var pc in map.Elements ("PC")) {
						var pd = new MidiProgramDefinition () { Name = pc.Attr ("Name"), Index = pc.AttrAsInt ("PC") - 1 }; // the domino XML index begins with 1 up to 128, so decrease here.
						mad.Programs.Add (pd);
						foreach (var bank in pc.Elements ("Bank")) {
							if (bank.Attr ("MSB") == null)
								// Domino XML definition contains extra bank element that mimics mapping. We have to skip it.
								continue;
							pd.Banks.Add (new MidiBankDefinition () {  Name = bank.Attr ("Name"), Msb = bank.AttrAsInt ("MSB"), Lsb = bank.AttrAsInt ("LSB") });
						}
					}
				}
		}
	}
}
