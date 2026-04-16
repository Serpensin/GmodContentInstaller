.PHONY: all run clean publish-linux publish-windows publish-all publish-appimage test-structure test-clean help

help:
	@echo "GMod Content Wizard - Makefile"
	@echo ""
	@echo "Available targets:"
	@echo "  make run              - Run the application"
	@echo "  make publish-linux    - Build Linux executable (native)"
	@echo "  make publish-windows  - Build Windows executable"
	@echo "  make publish-appimage - Build Linux AppImage"
	@echo "  make publish-all      - Build all versions (Linux + Windows)"
	@echo "  make test-structure   - Create test structure for path detection"
	@echo "  make test-clean       - Delete test structure"
	@echo "  make clean            - Clean build files"
	@echo ""
	@echo "Default target: help"

all: help

run:
	dotnet run

# Linux single file self-contained
publish-linux:
	dotnet publish -c Release -r linux-x64 --no-self-contained -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./dist
	mv dist/GModContentWizard dist/GModContentWizard.run

# Linux AppImage
publish-appimage:
	@echo "Building AppImage for Linux..."
	@if ! command -v appimagetool &> /dev/null; then \
		echo "Error: appimagetool not found in PATH"; \
		echo "Please install appimagetool first: https://github.com/AppImage/appimagetool"; \
		exit 1; \
	fi
	@rm -rf ./dist/AppImage
	@mkdir -p ./dist/AppImage/usr/bin
	dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./dist/AppImage/usr/bin
	mv ./dist/AppImage/usr/bin/GModContentWizard ./dist/AppImage/usr/bin/GModContentWizard.bin
	@echo '#!/bin/bash' > ./dist/AppImage/AppRun
	@echo 'exec "$$(dirname "$$0")/usr/bin/GModContentWizard.bin" "$$@"' >> ./dist/AppImage/AppRun
	@chmod +x ./dist/AppImage/AppRun
	@echo '[Desktop Entry]' > ./dist/AppImage/gmod-content-wizard.desktop
	@echo 'Name=GMod Content Wizard' >> ./dist/AppImage/gmod-content-wizard.desktop
	@echo 'Comment=Install Garry'"'"'s Mod content' >> ./dist/AppImage/gmod-content-wizard.desktop
	@echo 'Exec=GModContentWizard.bin' >> ./dist/AppImage/gmod-content-wizard.desktop
	@echo 'Type=Application' >> ./dist/AppImage/gmod-content-wizard.desktop
	@echo 'Categories=Game;' >> ./dist/AppImage/gmod-content-wizard.desktop
	@echo 'Terminal=false' >> ./dist/AppImage/gmod-content-wizard.desktop
	@echo 'Icon=gmod-content-wizard' >> ./dist/AppImage/gmod-content-wizard.desktop
	@mkdir -p ./dist/AppImage/usr/share/icons/hicolor/256x256/apps
	@if [ -f Resources/Logo.png ]; then \
		cp Resources/Logo.png ./dist/AppImage/gmod-content-wizard.png; \
		cp Resources/Logo.png ./dist/AppImage/usr/share/icons/hicolor/256x256/apps/gmod-content-wizard.png; \
	fi
	@mkdir -p ./dist/AppImage/usr/share/applications
	@cp ./dist/AppImage/gmod-content-wizard.desktop ./dist/AppImage/usr/share/applications/
	@appimagetool ./dist/AppImage ./dist/GModContentWizard.AppImage
	@rm -rf ./dist/AppImage
	@echo "Done: dist/GModContentWizard.AppImage"

# Windows single file self-contained
publish-windows:
	dotnet publish -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./dist

publish-all: publish-linux publish-windows publish-appimage
# Teststruktur für Pfaderkennung erstellen
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

# Test-Struktur löschen
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
	rm -rf bin obj dist