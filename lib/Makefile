CSCOMPILE = gmcs -pkg:wcf -r:System.Xml.Linq
TOPDIR = ..

target = $(TOPDIR)/build/Commons.Music.Midi.dll

target_sources = \
	base/SMF.cs \
	base/MidiPlayer.cs \
	base/MidiMachine.cs \
	portmidi-sharp/PortMidiSharp.cs \
	portmidi-sharp/PortMidiPlayer.cs \
	module-database/MidiModuleDatabase.cs

all: $(target)

$(target): $(target_sources)
	mkdir -p $(TOPDIR)/build
	$(CSCOMPILE) -out:$(target) -t:library $(target_sources)

clean:
	rm -rf $(target)
