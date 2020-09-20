using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Commons.Music.Midi
{
	public class MergedMidiModuleDatabase : MidiModuleDatabase
	{
		public MergedMidiModuleDatabase(IEnumerable<MidiModuleDatabase> sources)
		{
			List = new List<MidiModuleDatabase>();
		}

		public IList<MidiModuleDatabase> List { get; private set; }

		public override IEnumerable<MidiModuleDefinition> All() => List.SelectMany(d => d.All());

		public override MidiModuleDefinition Resolve(string moduleName)
		{
			return List.Select(d => d.Resolve(moduleName)).FirstOrDefault(m => m != null);
		}
	}
}
