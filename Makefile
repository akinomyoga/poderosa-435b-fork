# -*- Makefile -*-

.PHONY: all
all:

dist-exclude:= \
	--exclude=./Poderosa-4.3.5b/dist \
	--exclude=*/bin/* \
	--exclude=*/obj/*

dist435:
	date=$$(date +%Y%m%d-%H%M%S) && \
	tar cavf Poderosa-4.3.5b_mwg.$$date.tar.xz ./Poderosa-4.3.5b/ $(dist-exclude) && \
	cd Poderosa-4.3.5b/Executable/bin/ && \
	tar cavf ../../../Poderosa-4.3.5b_mwg.$$date-bin.tar.xz Release --exclude=*.pdb --exclude=*.xml --exclude=*.vshost.*
