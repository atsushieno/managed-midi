using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Commons.Music.Midi
{
	public class DefaultMidiModuleDatabase : MidiModuleDatabase
	{
		static readonly Assembly ass = typeof(DefaultMidiModuleDatabase).GetTypeInfo().Assembly;

		// am too lazy to adjust resource names :/
		public static Stream GetResource(string name)
		{
			return ass.GetManifestResourceStream(ass.GetManifestResourceNames().FirstOrDefault(m => m.EndsWith(name, StringComparison.OrdinalIgnoreCase)));
		}

		public DefaultMidiModuleDatabase()
		{
			Modules = new List<MidiModuleDefinition>();
			var catalog = new StreamReader(GetResource("midi-module-catalog.txt")).ReadToEnd().Split('\n');
			foreach (string filename in catalog.Select(s => s.Trim())) // strip extraneous \r
				if (filename.Length > 0)
					Modules.Add(MidiModuleDefinition.Load(GetResource(filename)));
		}

		public override IEnumerable<MidiModuleDefinition> All() => Modules;

		public override MidiModuleDefinition Resolve(string moduleName)
		{
			if (moduleName == null)
				return null;
			string name = ResolvePossibleAlias(moduleName);
			return Modules.FirstOrDefault(m => m.Name == name) ?? Modules.FirstOrDefault(m => m.Match != null && new Regex(m.Match).IsMatch(name) || name.Contains(m.Name));
		}

		public string ResolvePossibleAlias(string name)
		{
			switch (name)
			{
				case "Microsoft GS Wavetable Synth":
					return "Microsoft GS Wavetable SW Synth";
			}
			return name;
		}

		public IList<MidiModuleDefinition> Modules { get; private set; }
	}

}
