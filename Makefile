.PHONY: all run clean publish-linux publish-windows publish-all

all: run

run:
	dotnet run

# Linux single file self-contained
publish-linux:
	rm -rf dist
	dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./dist
	mv dist/GModContentWizard dist/GModContentWizard.run

# Windows single file self-contained
publish-windows:
	rm -rf dist
	dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./dist

# Alle Builds
publish-all: publish-linux publish-windows

clean:
	dotnet clean
	rm -rf bin obj dist