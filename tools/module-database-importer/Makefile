TOPDIR = ../..
CSCOMPILE = mcs -r:System.Xml.Linq.dll
RUNTIME = mono

target = $(TOPDIR)/build/domino-importer.exe
target_sources = domino-importer.cs

all: $(target)

$(target): $(target_sources)
	$(CSCOMPILE) -out:$(target) $(target_sources) -r:$(TOPDIR)/build/Commons.Music.Midi.dll

clean:
	rm -rf $(target)

test-import: $(target)
	for fff in *.xml ; do $(RUNTIME) $(target) $$fff ; done;


