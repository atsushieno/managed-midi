SUBDIRS = lib tools

.PHONY: subdirs $(SUBDIRS)
subdirs: $(SUBDIRS)
$(SUBDIRS):
	$(MAKE) -C $@

.PHONY: clean $(SUBDIRS)
clean:
	cd lib; $(MAKE) clean; cd ..
	cd tools; $(MAKE) clean; cd ..
