CSCOMPILE = mcs -pkg:wcf -r:System.Xml.Linq -debug
TOPDIR = ..

target = $(TOPDIR)/build/Commons.Music.Midi.dll

target_sources = \
	base/SMF.cs \
	base/MidiPlayer.cs \
	base/MidiMachine.cs \
	portmidi-sharp/PortMidiSharp.cs \
	portmidi-sharp/PortMidiPlayer.cs \
	rtmidi-sharp/RtMidiSharp.cs \
	rtmidi-sharp/RtMidiPlayer.cs \
	module-database/MidiModuleDatabase.cs

target_resources = \
	module-database/data/midi-module-catalog.txt \
	module-database/data/ms_gs_wavetable_synth.midimod \
	module-database/data/roland_sc-88.midimod \
	module-database/data/roland_sc-8820.midimod \
	module-database/data/roland_sc-88pro.midimod \
	module-database/data/yamaha_motif-rack_es.midimod

all: $(target)

$(target): $(target_sources) $(target_resources)
	mkdir -p $(TOPDIR)/build
	$(CSCOMPILE) -out:$(target) -t:library $(target_sources) \
		$(target_resources:%=/resource:%)

clean:
	rm -rf $(target) $(target).mdb

