# GModContentWizard

A desktop application for simplifying the installation of Garry's Mod content (maps, models, textures).

## Features

- **Automatic Detection**: Automatically locates your Garry's Mod addons folder on Windows and Linux
- **Server Selection**: Tests both primary and secondary servers and selects the one with lower ping
- **Drive Usage**: Real-time display of available disk space with cumulative size preview
- **Smart Toggle**: Shows installation status, download size vs installed size, and enables/disables content based on server reachability
- **Cross-Platform**: Supports both Windows and Linux

## Supported Content

- Counter-Strike: Source (Content & Maps)
- Day of Defeat (Content & Maps)
- Half-Life 1 (Content & Maps)
- Half-Life 2 Episode 1 (Content & Maps)
- Half-Life 2 Episode 2 (Content & Maps)
- Half-Life 2 Extras (Content & Maps)
- Portal (Content & Maps)
- Portal 2 (Content & Maps)
- Team Fortress 2 (Content & Maps)
- Left 4 Dead (Content & Maps)
- Left 4 Dead 2 (Content & Maps)

## Requirements

- Windows 10/11 or Linux
- Internet connection for downloading content
- Garry's Mod installed via Steam

### Runtime Requirements

- **Windows Self-Contained**: No runtime needed (includes .NET)
- **Windows Framework-Dependent**: .NET 10 runtime
- **Linux Self-Contained**: No runtime needed (includes .NET)
- **Linux Framework-Dependent**: .NET 10 runtime
- **Linux AppImage**: Ubuntu 18.04 minimum (glibc 2.27+ required)

## Installation

### Option 1: Pre-built Releases

Download the latest release from the [releases page](https://github.com/Serpensin/GmodContentInstaller/releases):

| Platform | Type | Filename | Size |
|----------|------|----------|------|
| Windows | Self-Contained | GModContentWizard-sc.exe | ~100MB |
| Windows | Framework-Dependent | GModContentWizard.exe | ~30MB |
| Linux | Self-Contained | GModContentWizard-sc.run | ~100MB |
| Linux | Framework-Dependent | GModContentWizard.run | ~30MB |
| Linux | AppImage | GMod-Content-Wizard-2.0.0-x86_64.AppImage | ~40MB |

### Option 2: Build from Source

Prerequisites:
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10)
- For AppImage only: [appimagetool](https://github.com/AppImage/appimagetool)

#### Clone and Build

```bash
# Clone the repository
git clone https://github.com/Serpensin/GmodContentInstaller.git
cd GmodContentInstaller

# Restore dependencies
dotnet restore

# Build debug
dotnet build
```

#### Running

```bash
# Run the application
dotnet run

# Or use make
make run
```

#### Building for Release

The easiest way is using the Makefile:

```bash
# Show all available targets
make help

# Build Linux framework-dependent single file (~30MB)
# Output: dist/GModContentWizard.run
make publish-linux

# Build Linux self-contained single file (~100MB)
# Output: dist/GModContentWizard-sc.run
make publish-linux-sc

# Build Windows framework-dependent single file (~30MB)
# Output: dist/GModContentWizard.exe
make publish-windows

# Build Windows self-contained single file (~100MB)
# Output: dist/GModContentWizard-sc.exe
make publish-windows-sc

# Build Linux AppImage (~100MB)
# This runs appdir-lint.sh automatically to validate the AppImage
# Output: dist/GMod-Content-Wizard-2.0.0-x86_64.AppImage
make publish-appimage

# Check AppImage manually without packaging
# Useful for debugging AppImage issues
make check-appdir

# Build all versions
# Output: dist/ (all versions)
make publish-all
```

##### Manual Build Commands

If you don't want to use the Makefile:

```bash
# Linux framework-dependent
dotnet publish -c Release -r linux-x64 --no-self-contained -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./disttemp
cp ./disttemp/GModContentWizard ./dist/GModContentWizard.run
rm -rf ./disttemp

# Linux self-contained
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./disttemp
cp ./disttemp/GModContentWizard ./dist/GModContentWizard-sc
rm -rf ./disttemp

# Windows framework-dependent
dotnet publish -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./disttemp
cp ./disttemp/GModContentWizard.exe ./dist/GModContentWizard.exe
rm -rf ./disttemp

# Windows self-contained
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./disttemp
cp ./disttemp/GModContentWizard.exe ./dist/GModContentWizard-sc.exe
rm -rf ./disttemp
```

For AppImage, the Makefile builds the AppDir first, runs validation, then packages it:

```bash
# 1. Build AppDir with self-contained runtime
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./dist/AppImage/usr/bin

# 2. Setup AppRun and desktop file (see Makefile for details)

# 3. Validate with appdir-lint.sh
curl -fsSL https://raw.githubusercontent.com/AppImageCommunity/pkg2appimage/refs/heads/master/appdir-lint.sh -o ./appdir-lint.sh
curl -fsSL https://raw.githubusercontent.com/AppImageCommunity/pkg2appimage/refs/heads/master/excludelist -o ./excludelist
chmod +x ./appdir-lint.sh
./appdir-lint.sh ./dist/AppImage

# 4. Package with appimagetool
appimagetool ./dist/AppImage ./dist/GMod-Content-Wizard-2.0.0-x86_64.AppImage
```

## Usage

1. **Launch**: Run GModContentWizard.exe (Windows) or GModContentWizard (Linux/AppImage)
2. **Detect Path**: Click "Detect Path" to automatically find your Garry's Mod addons folder, or the application will prompt you to select it manually
3. **Select Content**: Toggle the content you want to install using the switches
   - Green text = Already installed
   - Red text = Server unreachable (content unavailable)
4. **Download**: Click "Download/Update" to download selected content
5. **Play**: Click "Launch GMod" to open Garry's Mod via Steam

For Linux AppImage:
```bash
chmod +x GMod-Content-Wizard-2.0.0-x86_64.AppImage
./GMod-Content-Wizard-2.0.0-x86_64.AppImage
```

## Project Structure

```
GmodContentInstaller/
├── Program.cs                  # Application entry point with logging setup
├── App.axaml.cs                # Avalonia application class
├── MainWindow.axaml.cs         # Main window logic
├── ContentInfo.cs              # Data model for content information
├── Downloader.cs               # File download handling
├── ArchiveExtractor.cs         # Zip/tar.gz extraction
├── PathDetector.cs             # Garry's Mod folder detection
├── DriveUsageUpdater.cs        # Disk space calculations
├── UrlChecker.cs               # URL reachability checking
├── Resources/
│   ├── urls.json               # Content URLs and sizes
│   └── *.png                   # Game icons
└── Makefile                    # Build automation
```

## Technologies

- [.NET 10](https://dotnet.microsoft.com/)
- [Avalonia UI](https://avaloniaui.net/) - Cross-platform .NET UI framework
- [SharpCompress](https://github.com/adamhathcock/sharpcompress) - Archive handling
- [Serilog](https://serilog.net/) - Logging

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

## License

MIT License - See [LICENSE](LICENSE.txt) for details.

## Contact

- Discord: [Join our server](https://url.serpensin.com/discord)
- Issues: [GitHub Issues](https://github.com/Serpensin/GmodContentInstaller/issues)