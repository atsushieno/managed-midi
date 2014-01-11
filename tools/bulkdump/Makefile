TOPDIR = ../..
CSCOMPILE = mcs -pkg:dotnet

target = $(TOPDIR)/build/bulkdump-managed.exe
target_sources = bulkdump.cs

all: $(target)

$(target): $(target_sources)
	$(CSCOMPILE) -out:$(target) $(target_sources) -r:$(TOPDIR)/build/Commons.Music.Midi.dll

clean:
	rm -rf $(target)
