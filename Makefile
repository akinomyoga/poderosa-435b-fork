# -*- Makefile -*-

.PHONY: all
all:

dist435: dist-source dist-binary

dist-source-exclude := \
  --exclude=./Poderosa-4.3.5b/dist \
  --exclude=./Poderosa-4.3.5b/_upgrade \
  --exclude=\*.suo \
  --exclude=bin \
  --exclude=obj
dist-source:
	date=$$(date +%Y%m%d-%H%M%S) && \
	tar cavf Poderosa-4.3.5b_mwg.$$date.tar.xz $(dist-source-exclude) ./Poderosa-4.3.5b/

dist-binary-exclude := \
  --exclude=*.pdb --exclude=*.xml --exclude=*.vshost.* \
  --exclude=cygterm/cygterm.*.exe \
  --transform=s:^Release:./Poderosa-$${date%%-*}:
dist-binary:
	date=$$(date +%Y%m%d-%H%M%S) && \
	cd Poderosa-4.3.5b/Executable/bin/ && \
	tar cavf ../../../Poderosa-4.3.5b_mwg.$$date-bin.tar.xz $(dist-binary-exclude) Release
