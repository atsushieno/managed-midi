using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Xml;

namespace Commons.Music.Midi
{
	public abstract class MidiModuleDatabase
	{
		public static readonly MidiModuleDatabase Default = new DefaultMidiModuleDatabase ();

		public abstract IEnumerable<MidiModuleDefinition> All ();
		
		public abstract MidiModuleDefinition Resolve (string moduleName);
	}

}
