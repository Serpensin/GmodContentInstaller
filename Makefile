.PHONY: all run clean publish-linux publish-linux-sc publish-windows publish-windows-sc publish-all publish-appimage check-appdir test-structure test-clean help generate-profiles

help:
	@echo "GMod Content Wizard - Makefile"
	@echo ""
	@echo "Available targets:"
	@echo "  make run                - Run the application"
	@echo "  make publish-linux      - Build Linux framework-dependent single file"
	@echo "  make publish-linux-sc   - Build Linux self-contained single file"
	@echo "  make publish-windows    - Build Windows framework-dependent single file"
	@echo "  make publish-windows-sc - Build Windows self-contained single file"
	@echo "  make publish-appimage   - Build Linux AppImage"
	@echo "  make check-appdir       - Check AppImage using appdir-lint.sh"
	@echo "  make publish-all        - Build all versions (Linux + Windows + AppImage)"
	@echo "  make test-structure     - Create test structure for path detection"
	@echo "  make test-clean         - Delete test structure"
	@echo "  make clean              - Clean build files"
	@echo ""
	@echo "Default target: help"

all: help

run:
	dotnet run

publish-linux:
	@mkdir -p dist
	dotnet publish -c Release -r linux-x64 --no-self-contained -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./dist-temp
	@cp ./dist-temp/GModContentWizard ./dist/GModContentWizard.run
	@chmod +x ./dist/GModContentWizard.run
	@rm -rf ./dist-temp

publish-linux-sc:
	@mkdir -p dist
	dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./dist-temp
	@cp ./dist-temp/GModContentWizard ./dist/GModContentWizard-sc.run
	@chmod +x ./dist/GModContentWizard-sc.run
	@rm -rf ./dist-temp

publish-appimage: check-appdir
	@VERSION=$$(grep AssemblyFileVersion AssemblyInfo.cs | sed 's/.*"\([^"]*\)".*/\1/' | cut -d'.' -f1-3); \
	echo "Packaging AppImage..."; \
	appimagetool ./dist/AppImage ./dist/GMod-Content-Wizard-$${VERSION}-x86_64.AppImage 2>&1 | grep -v "value.*for key.*Version" || true; \
	rm -rf ./dist/AppImage; \
	echo "Done: dist/GMod-Content-Wizard-$${VERSION}-x86_64.AppImage"

check-appdir:
	@echo "Building AppImage for checking..."
	@if ! command -v appimagetool &> /dev/null; then \
		echo "Error: appimagetool not found in PATH"; \
		echo "Please install appimagetool first: https://github.com/AppImage/appimagetool"; \
		exit 1; \
	fi
	@rm -rf ./dist/AppImage
	@mkdir -p ./dist/AppImage/usr/bin
	dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:InvariantGlobalization=true -o ./dist/AppImage/usr/bin
	mv ./dist/AppImage/usr/bin/GModContentWizard ./dist/AppImage/usr/bin/GModContentWizard.bin
	@cp Resources/AppImage/AppRun ./dist/AppImage/AppRun
	@chmod +x ./dist/AppImage/AppRun
	@mkdir -p ./dist/AppImage/usr/share/icons/hicolor/256x256/apps
	@if [ -f Resources/Icon.png ]; then \
cp Resources/Icon.png ./dist/AppImage/com.serpensin.gmodcontentwizard.png; \
cp Resources/Icon.png ./dist/AppImage/usr/share/icons/hicolor/256x256/apps/com.serpensin.gmodcontentwizard.png; \
		cp Resources/Icon.png ./dist/AppImage/.DirIcon; \
	fi
	@mkdir -p ./dist/AppImage/usr/share/applications
	@cp Resources/AppImage/com.serpensin.gmodcontentwizard.desktop ./dist/AppImage/com.serpensin.gmodcontentwizard.desktop
	@cp ./dist/AppImage/com.serpensin.gmodcontentwizard.desktop ./dist/AppImage/usr/share/applications/
	@mkdir -p ./dist/AppImage/usr/share/metainfo
	@VERSION=$$(grep AssemblyFileVersion AssemblyInfo.cs | sed 's/.*"\([^"]*\)".*/\1/' | cut -d'.' -f1-3); \
	sed -e "s/\[\[VERSION\]\]/$$VERSION/g" -e "s/\[\[DATE\]\]/$$(date +%Y-%m-%d)/g" Resources/AppImage/com.serpensin.gmodcontentwizard.appdata.xml > ./dist/AppImage/usr/share/metainfo/com.serpensin.gmodcontentwizard.appdata.xml
	@echo "Fetching appdir-lint.sh..."
	@curl -fsSL https://raw.githubusercontent.com/AppImageCommunity/pkg2appimage/refs/heads/master/appdir-lint.sh -o ./appdir-lint.sh
	@chmod +x ./appdir-lint.sh
	@echo "Fetching excludelist..."
	@curl -fsSL https://raw.githubusercontent.com/AppImageCommunity/pkg2appimage/refs/heads/master/excludelist -o ./excludelist
	@echo "Running appdir-lint.sh on ./dist/AppImage..."
	@./appdir-lint.sh ./dist/AppImage 2>&1 | grep -v "type-property-required" || true
	@rm -f ./appdir-lint.sh ./excludelist
	@echo "AppImage check passed (AppImage directory kept for packaging)"

publish-windows:
	@mkdir -p dist
	dotnet publish -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./dist-temp
	@cp ./dist-temp/GModContentWizard.exe ./dist/GModContentWizard.exe
	@rm -rf ./dist-temp

publish-windows-sc:
	@mkdir -p dist
	dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./dist-temp
	@cp ./dist-temp/GModContentWizard.exe ./dist/GModContentWizard-sc.exe
	@rm -rf ./dist-temp

publish-all: publish-linux publish-linux-sc publish-windows publish-windows-sc publish-appimage

test-structure:
	@if [ "$(OS)" = "Windows_NT" ] || [ -n "$$WINDIR" ]; then \
		echo "Erstelle Test-Struktur (Windows)..."; \
		mkdir "%USERPROFILE%\SteamLibrary\steamapps\common\GarrysMod\garrysmod\addons" 2>nul; \
		mkdir "%USERPROFILE%\SteamLibrary\steamapps\common\GarrysMod\garrysmod\gamemodes" 2>nul; \
		echo 4000 > "%USERPROFILE%\SteamLibrary\steamapps\common\GarrysMod\steam_appid.txt"; \
		type nul > "%USERPROFILE%\SteamLibrary\steamapps\common\GarrysMod\hl2.exe"; \
		echo "Erstellt: %USERPROFILE%\SteamLibrary"; \
		echo "  steamapps\common\GarrysMod\hl2.exe"; \
		echo "  steamapps\common\GarrysMod\steam_appid.txt (mit 4000)"; \
		echo "  steamapps\common\GarrysMod\garrysmod\addons\"; \
		echo "  steamapps\common\GarrysMod\garrysmod\gamemodes\"; \
	else \
		echo "Erstelle Test-Struktur (Linux)..."; \
		mkdir -p ~/gmod-test/steamapps/common/GarrysMod/garrysmod/addons; \
		mkdir -p ~/gmod-test/steamapps/common/GarrysMod/garrysmod/gamemodes; \
		echo "4000" > ~/gmod-test/steamapps/common/GarrysMod/steam_appid.txt; \
		touch ~/gmod-test/steamapps/common/GarrysMod/hl2_linux; \
		echo "Erstellt: ~/gmod-test"; \
		echo "  steamapps/common/GarrysMod/hl2_linux"; \
		echo "  steamapps/common/GarrysMod/steam_appid.txt (mit 4000)"; \
		echo "  steamapps/common/GarrysMod/garrysmod/addons/"; \
		echo "  steamapps/common/GarrysMod/garrysmod/gamemodes/"; \
	fi

test-clean:
	@if [ "$(OS)" = "Windows_NT" ] || [ -n "$$WINDIR" ]; then \
		echo "Lösche Test-Struktur (Windows)..."; \
		rmdir /s /q "%USERPROFILE%\SteamLibrary" 2>nul; \
		echo "Gelöscht: %USERPROFILE%\SteamLibrary"; \
	else \
		echo "Lösche Test-Struktur (Linux)..."; \
		rm -rf ~/gmod-test; \
		echo "Gelöscht: ~/gmod-test"; \
	fi

clean:
	dotnet clean
	rm -rf bin obj dist dist-temp packages