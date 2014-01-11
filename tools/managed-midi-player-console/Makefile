TOPDIR = ../..
CSCOMPILE = mcs -pkg:dotnet

target = $(TOPDIR)/build/managed-midi-player-console.exe
target_sources = managed-midi-player-console.cs

all: $(target)

$(target): $(target_sources)
	$(CSCOMPILE) -out:$(target) $(target_sources) -r:$(TOPDIR)/build/Commons.Music.Midi.dll

clean:
	rm -rf $(target)
