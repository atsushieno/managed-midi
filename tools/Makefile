SUBDIRS = \
	managed-midi-player-console \
	mmk \
	bulkdump \
	module-database-importer

.PHONY: subdirs $(SUBDIRS)

subdirs: $(SUBDIRS)

$(SUBDIRS):
	$(MAKE) -C $@

clean:
	for subdir in $(SUBDIRS); do cd $$subdir; $(MAKE) clean; cd ..; done;

