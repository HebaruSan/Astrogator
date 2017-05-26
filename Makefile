.PHONY: all clean

SOURCEDIR=src
SOURCE=$(wildcard $(SOURCEDIR)/*.cs) $(wildcard $(SOURCEDIR)/*.csproj)
ASSETDIR=assets
ICONS=$(wildcard $(ASSETDIR)/*.png)
CONFIGS=$(wildcard $(ASSETDIR)/*.cfg)
LANGUAGES=$(ASSETDIR)/lang
README=README.md
GAMELINK=$(SOURCEDIR)/KSP_x64_Data
DEFAULTGAMEDIR=$(HOME)/.local/share/Steam/SteamApps/common/Kerbal Space Program

DEBUGDLL=$(SOURCEDIR)/bin/Debug/Astrogator.dll
RELEASEDLL=$(SOURCEDIR)/bin/Release/Astrogator.dll
DISTDIR=Astrogator
RELEASEZIP=Astrogator.zip
DLLDOCS=$(SOURCEDIR)/bin/Release/Astrogator.xml
DLLSYMBOLS=$(DEBUGDLL).mdb
LICENSE=LICENSE
VERSION=Astrogator.version
TAGS=tags

TARGETS=$(DEBUGDLL) $(RELEASEDLL) $(RELEASEZIP)

all: $(TAGS) $(TARGETS)

$(TAGS): $(SOURCE)
	ctags -f $@ $^

$(DLLSYMBOLS): $(DEBUGDLL)

$(DLLDOCS): $(RELEASEDLL)

$(DEBUGDLL): $(SOURCE) $(GAMELINK)
	cd $(SOURCEDIR) && xbuild /p:Configuration=Debug

$(RELEASEDLL): $(SOURCE) $(GAMELINK)
	cd $(SOURCEDIR) && xbuild /p:Configuration=Release

$(RELEASEZIP): $(RELEASEDLL) $(ICONS) $(README) $(DLLDOCS) $(DLLSYMBOLS) $(LICENSE) $(VERSION) $(CONFIGS) $(LANGUAGES)
	mkdir -p $(DISTDIR)
	cp -a $^ $(DISTDIR)
	zip -r $@ $(DISTDIR) -x \*.settings

$(GAMELINK):
	if [ -x "$(DEFAULTGAMEDIR)" ]; \
	then \
		ln -s "$(DEFAULTGAMEDIR)"/KSP_x64_Data $(GAMELINK); \
	else \
		echo "$(GAMELINK) not found."; \
		echo 'This must be a symlink to Kerbal Space Program/KSP_x64_Data.'; \
		exit 2; \
	fi

clean:
	cd $(SOURCEDIR) && xbuild /t:Clean
	rm -f $(TARGETS) $(TAGS)
	rm -rf $(SOURCEDIR)/bin $(SOURCEDIR)/obj $(DISTDIR)
