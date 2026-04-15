.PHONY: all run clean publish-linux publish-windows publish-all test-structure test-clean help

help:
	@echo "GMod Content Wizard - Makefile"
	@echo ""
	@echo "Available targets:"
	@echo "  make run             - Run the application"
	@echo "  make publish-linux   - Build Linux executable"
	@echo "  make publish-windows - Build Windows executable"
	@echo "  make publish-all     - Build both executables"
	@echo "  make test-structure  - Create test structure for path detection"
	@echo "  make test-clean      - Delete test structure"
	@echo "  make clean           - Clean build files"
	@echo ""
	@echo "Default target: help"

all: help

run:
	dotnet run

# Linux single file self-contained
publish-linux:
	dotnet publish -c Release -r linux-x64 --no-self-contained -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./dist
	mv dist/GModContentWizard dist/GModContentWizard.run

# Windows single file self-contained
publish-windows:
	dotnet publish -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./dist

# Alle Builds
publish-all: publish-linux publish-windows

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