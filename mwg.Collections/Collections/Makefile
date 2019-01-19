# -*- GNUmakefile -*-

all: BList.gen.cs
.PHONY: all
BList.gen.cs: BList.pp.cs
	PPC_CPP=1 mwg_pp.awk $< > $@
