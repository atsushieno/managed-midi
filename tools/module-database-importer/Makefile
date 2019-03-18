TOPDIR = ../..
CSCOMPILE = csc -debug -r:System.Xml.Linq.dll
RUNTIME = mono

LIB_DLL = $(TOPDIR)/Commons.Music.Midi.Desktop/bin/Debug/Commons.Music.Midi.dll

target = domino-importer.exe
target_sources = domino-importer.cs

all: $(target)

$(target): $(target_sources)
	cp $(LIB_DLL) .
	$(CSCOMPILE) -out:$(target) $(target_sources) -r:Commons.Music.Midi.dll

clean:
	rm -rf $(target)

test-import: $(target)
	for fff in *.xml ; do $(RUNTIME) $(target) $$fff ; done;


