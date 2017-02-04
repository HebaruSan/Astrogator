.PHONY: all clean

SOURCEDIR=src
SOURCE=$(wildcard $(SOURCEDIR)/*.cs)
ASSETDIR=assets
ICONS=$(wildcard $(ASSETDIR)/*.png)
README=README.md
GAMELINK=$(SOURCEDIR)/KSP_x64_Data
DEFAULTGAMEDIR="$(HOME)/.local/share/Steam/SteamApps/common/Kerbal Space Program"

DEBUGDLL=$(SOURCEDIR)/bin/Debug/Astrogator.dll
RELEASEDLL=$(SOURCEDIR)/bin/Release/Astrogator.dll
DISTDIR=Astrogator
RELEASEZIP=Astrogator.zip
DLLDOCS=$(SOURCEDIR)/bin/Release/Astrogator.xml
DLLSYMBOLS=$(DEBUGDLL).mdb
LICENSE=LICENSE

TARGETS=$(DEBUGDLL) $(RELEASEDLL) $(RELEASEZIP)

all: $(TARGETS)

$(DEBUGDLL): $(SOURCE) $(GAMELINK)
	cd $(SOURCEDIR) && xbuild /p:Configuration=Debug

$(RELEASEDLL): $(SOURCE) $(GAMELINK)
	cd $(SOURCEDIR) && xbuild /p:Configuration=Release

$(RELEASEZIP): $(RELEASEDLL) $(ICONS) $(README) $(DLLDOCS) $(DLLSYMBOLS) $(LICENSE)
	mkdir -p $(DISTDIR)
	cp $^ $(DISTDIR)
	zip -r $@ $(DISTDIR)

$(GAMELINK):
	if [ -x "$(DEFAULTGAMEDIR)" ]; \
	then; \
		ln -s "$(DEFAULTGAMEDIR)"/KSP_x64_Data $(GAMELINK); \
	else; \
		echo "$(GAMELINK) not found."; \
		echo 'This must be a symlink to Kerbal Space Program/KSP_x64_Data.'; \
		exit 2; \
	fi

clean:
	cd $(SOURCEDIR) && xbuild /t:Clean
	rm -f $(TARGETS)
	rm -rf $(SOURCEDIR)/bin $(SOURCEDIR)/obj $(DISTDIR)
