CSCOMPILE = mcs -pkg:dotnet -debug
TOPDIR = ../..

mmk = $(TOPDIR)/build/mmk.exe 
mmchromakey = $(TOPDIR)/build/mmchromakey.exe

common_opts = -resource:tonelist.txt,tonelist.txt -r:$(TOPDIR)/build/Commons.Music.Midi.dll

all: $(mmk) $(mmchromakey)

$(mmk) : mmk.cs
	$(CSCOMPILE) mmk.cs -out:$(mmk) $(common_opts)

$(mmchromakey) : mmk.cs
	$(CSCOMPILE) mmk.cs -out:$(mmchromakey) $(common_opts) -d:CHROMA_TONE

clean:
	rm -f $(mmk) $(mmk).mdb
	rm -f $(mmchromakey) $(mmchromakey).mdb

