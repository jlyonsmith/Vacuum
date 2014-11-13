CONFIG?=Release
PREFIX?=prefix
PREFIX:=$(abspath $(PREFIX))
VERSION=3.0.11113
PROJECT=Vacuum
STAGEDIR=Scratch/Homebrew
libFiles=$(PROJECT).exe $(PROJECT)Library.dll $(PROJECT).exe.config ToolBelt.dll MsgPack.dll TsonLibrary.dll
otherFiles=makefile README.md LICENSE.md template.sh
lc=$(shell echo $(1) | tr A-Z a-z)
zipFile=$(PROJECT)-$(VERSION).tar.gz

define copyRule
$(1): $(2)
	cp $$< $$@

endef

define mkDirRule
$(1):
	mkdir -p $$@

endef

.PHONY: default
default:
	$(error Specify clean, dist or install)

.PHONY: dist
dist: $(STAGEDIR) \
	  $(STAGEDIR)/lib \
	  $(foreach X,$(libFiles),$(STAGEDIR)/lib/$(X)) \
	  $(foreach X,$(otherFiles),$(STAGEDIR)/$(X)) \
	  $(zipFile)

$(STAGEDIR):
	mkdir -p $@

$(STAGEDIR)/lib: 
	mkdir $@

$(zipFile): $(foreach X,$(libFiles),$(STAGEDIR)/lib/$(X)) \
			$(foreach X,$(otherFiles),$(STAGEDIR)/$(X))
	tar -cvz -C $(STAGEDIR) -f $(zipFile) ./
	openssl sha1 $(zipFile)
	@echo "aws s3 cp" $(zipFile) "s3://jlyonsmith/ --profile jamoki --acl public-read"

$(foreach X,$(libFiles),$(eval $(call copyRule,$(STAGEDIR)/lib/$(X),$(PROJECT)/bin/$(CONFIG)/$(X))))
$(foreach X,$(otherFiles),$(eval $(call copyRule,$(STAGEDIR)/$(X),$(X))))

# NOTE: Test 'install' by going to STAGEDIR and running there!

.PHONY: install
install: $(PREFIX)/bin \
		 $(PREFIX)/lib \
		 $(foreach X,$(libFiles),$(PREFIX)/lib/$(X)) \
		 $(foreach X,$(otherFiles),$(PREFIX)/$(X)) \
		 $(PREFIX)/bin/$(call lc,$(PROJECT))
		 
$(PREFIX)/lib:
	mkdir -p $@

$(PREFIX)/bin:
	mkdir -p $@

$(PREFIX)/bin/$(call lc,$(PROJECT)): $(PREFIX)/template.sh
	sed -e 's,_TOOL_,$(PROJECT),g' -e 's,_PREFIX_,$(PREFIX),g' template.sh > $@
	chmod u+x $@

$(foreach X,$(libFiles),$(eval $(call copyRule,$(PREFIX)/lib/$(X),lib/$(X))))
$(foreach X,$(otherFiles),$(eval $(call copyRule,$(PREFIX)/$(X),$(X))))

.PHONY: clean
clean:
	-@rm *.gz
	-@rm -rf $(STAGEDIR)
