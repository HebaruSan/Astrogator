.PHONY: all clean install-dev test s3-deps

PROJECT:=Astrogator

SOURCEDIR:=Source
SOURCE:=$(wildcard $(SOURCEDIR)/*.cs $(SOURCEDIR)/*/*.cs $(SOURCEDIR)/*.csproj)
GAMELINK:=$(SOURCEDIR)/KSP_Data
DEFAULTGAMEDIR:=$(HOME)/.local/share/Steam/steamapps/common/Kerbal Space Program

DISTDIR:=GameData/$(PROJECT)
STATICS:=$(wildcard $(DISTDIR)/* $(DISTDIR)/*/*)
DLL:=$(DISTDIR)/Plugins/$(PROJECT).dll
ZIP:=$(PROJECT).zip

TESTINGDIR:=$(DEFAULTGAMEDIR)/GameData/$(PROJECT)

all: $(ZIP)

$(ZIP): $(DLL) $(STATICS)
	msbuild /t:MakeZip

$(DLL): $(GAMELINK) $(SOURCE)
	msbuild /r

$(GAMELINK):
	if [ -x "$(DEFAULTGAMEDIR)" ]; \
	then \
		ln -s "$(DEFAULTGAMEDIR)"/KSP_Data $@; \
	else \
		echo "$@ not found."; \
		echo 'This must be a symlink to Kerbal Space Program/KSP_Data.'; \
		exit 2; \
	fi

clean:
	msbuild /t:Clean
	rm -f *.gpg

install-dev: $(TESTINGDIR)

$(TESTINGDIR):
	ln -sf "$$(pwd)/$(DISTDIR)" "$(DEFAULTGAMEDIR)/GameData"

test: $(TESTINGDIR) $(DLL)
	steam steam://rungameid/220200

ifdef GITHUB_TOKEN

# These use = instead of := to avoid setting them if DEFAULTGAMEDIR doesn't exist

KSP_VERSION=$(shell egrep -o '^Version [0-9]+\.[0-9]+' "$(DEFAULTGAMEDIR)"/readme.txt | awk '{print $$2}')
DEP_FILE=KSP_Data-$(KSP_VERSION).tar.gz.gpg
KSP_DEPENDS=$(shell hxselect -c -s '\n' HintPath < $(SOURCEDIR)/$(PROJECT).csproj | tr '\\' /)

$(DEP_FILE): $(GAMELINK)
	mkdir -p KSP_Data/Managed
	for F in $(KSP_DEPENDS); do cp -a "$(SOURCEDIR)/$$F" "$$F" ; done
	tar czf - KSP_Data | gpg --batch --passphrase $$GITHUB_TOKEN -c > $@
	rm -r KSP_Data

ifdef AWS_ACCESS_KEY_ID
ifdef AWS_SECRET_ACCESS_KEY
ifdef AWS_DEFAULT_REGION

s3-deps: $(DEP_FILE)
	aws s3 cp $^ s3://hebarusan/$^

endif
endif
endif

endif
