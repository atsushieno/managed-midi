using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace Commons.Music.Midi
{
	public abstract class MidiModuleDatabase
	{
		public static readonly MidiModuleDatabase Default = new DefaultMidiModuleDatabase ();
		
		public abstract MidiModuleDefinition Resolve (string moduleName);
	}

	public class MergedMidiModuleDatabase : MidiModuleDatabase
	{
		public MergedMidiModuleDatabase (IEnumerable<MidiModuleDatabase> sources)
		{
			List = new List<MidiModuleDatabase> ();
		}
		
		public IList<MidiModuleDatabase> List { get; private set; }
		
		public override MidiModuleDefinition Resolve (string moduleName)
		{
			return List.Select (d => d.Resolve (moduleName)).FirstOrDefault (m => m != null);
		}
	}
	
	class DefaultMidiModuleDatabase : MidiModuleDatabase
	{
		static readonly Assembly ass = typeof (DefaultMidiModuleDatabase).GetTypeInfo ().Assembly;

		// am too lazy to adjust resource names :/
		public static Stream GetResource (string name)
		{
			return ass.GetManifestResourceStream (name) ?? ass.GetManifestResourceStream ("module-database/data/" + name);
		}

		public DefaultMidiModuleDatabase ()
		{
			Modules = new List<MidiModuleDefinition> ();
			var catalog = new StreamReader (GetResource ("midi-module-catalog.txt")).ReadToEnd ().Split ('\n');
			foreach (string filename in catalog)
				if (filename.Length > 0)
					Modules.Add (MidiModuleDefinition.Load (GetResource (filename)));
		}

		public override MidiModuleDefinition Resolve (string moduleName)
		{
			if (moduleName == null)
				throw new ArgumentNullException ("moduleName");
			string name = ResolvePossibleAlias (moduleName);
			return Modules.FirstOrDefault (m => m.Name == name) ?? Modules.FirstOrDefault (m => name.Contains (m.Name));
		}

		public string ResolvePossibleAlias (string name)
		{
			switch (name) {
			case "Microsoft GS Wavetable Synth":
				return "Microsoft GS Wavetable SW Synth";
			}
			return name;
		}

		public IList<MidiModuleDefinition> Modules { get; private set; }
	}

	[DataContract]
	public class MidiModuleDefinition
	{
		public MidiModuleDefinition ()
		{
			Instrument = new MidiInstrumentDefinition ();
		}

		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public MidiInstrumentDefinition Instrument { get; set; }

		// serialization

		#if !PORTABLE
		public void Save (string file)
		{
			using (var fs = File.OpenWrite (file))
				Save (fs);
		}
		#endif

		public void Save (Stream stream)
		{
			var ds = new DataContractJsonSerializer (typeof (MidiModuleDefinition));
			ds.WriteObject (stream, this);
		}

		#if !PORTABLE
		public static MidiModuleDefinition Load (string file)
		{
			using (var fs = File.OpenRead (file))
				return Load (fs);
		}
		#endif

		public static MidiModuleDefinition Load (Stream stream)
		{
			var ds = new DataContractJsonSerializer (typeof (MidiModuleDefinition));
			return (MidiModuleDefinition) ds.ReadObject (stream);
		}
	}

	[DataContract]
	public class MidiInstrumentDefinition
	{
		public MidiInstrumentDefinition ()
		{
			Maps = new List<MidiInstrumentMap> ();
		}

		public IList<MidiInstrumentMap> Maps { get; private set; }

		[DataMember (Name = "Maps")]
		MidiInstrumentMap [] maps {
			get { return Maps.ToArray (); }
			set { Maps = new List<MidiInstrumentMap> (value); }
		}
	}

	[DataContract]
	public class MidiInstrumentMap
	{
		public MidiInstrumentMap ()
		{
			Programs = new List<MidiProgramDefinition> ();
		}
		
		[DataMember]
		public string Name { get; set; }

		public IList<MidiProgramDefinition> Programs { get; private set; }

		[DataMember (Name = "Programs")]
		MidiProgramDefinition [] programs {
			get { return Programs.ToArray (); }
			set { Programs = new List<MidiProgramDefinition> (value); }
		}
	}

	[DataContract]
	public class MidiProgramDefinition
	{
		public MidiProgramDefinition ()
		{
			Banks = new List<MidiBankDefinition> ();
		}

		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public int Index { get; set; }


		public IList<MidiBankDefinition> Banks { get; private set; }

		[DataMember (Name = "Banks")]
		MidiBankDefinition [] banks {
			get { return Banks.ToArray (); }
			set { Banks = new List<MidiBankDefinition> (value); }
		}
	}

	[DataContract]
	public class MidiBankDefinition
	{
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public int Msb { get; set; }
		[DataMember]
		public int Lsb { get; set; }
	}
}
