CONFIG?=Debug
files=Vacuum.Filters.dll Vacuum.Filters.dll.mdb

define copyrule
bin/$$(CONFIG)/$(1): ../Filters/bin/$$(CONFIG)/$(1)
	cp $$< $$@

endef

all: $(foreach x, $(files), bin/$(CONFIG)/$(x))

$(foreach x, $(files), $(eval $(call copyrule,$(x))))

