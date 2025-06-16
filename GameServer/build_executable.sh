#!/bin/bash

echo "🎮 Building GameServer Executable for Unity Integration..."
echo ""

# Clean previous builds
if [ -d "bin/Release/publish" ]; then
    echo "Cleaning previous builds..."
    rm -rf "bin/Release/publish"
fi

# Build for Windows x64 (self-contained)
echo "Building Windows x64 version..."
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "bin/Release/publish/win-x64"

# Build for Windows x86 (for older systems)
echo "Building Windows x86 version..."
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "bin/Release/publish/win-x86"

# Build for Linux x64 (for cross-platform)
echo "Building Linux x64 version..."
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "bin/Release/publish/linux-x64"

# Build for macOS x64
echo "Building macOS x64 version..."
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "bin/Release/publish/osx-x64"

# Build for macOS ARM64 (Apple Silicon)
echo "Building macOS ARM64 version..."
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "bin/Release/publish/osx-arm64"

echo ""
echo "✅ Build completed successfully!"
echo ""
echo "Executables created:"
echo "  Windows x64: bin/Release/publish/win-x64/GameServer.exe"
echo "  Windows x86: bin/Release/publish/win-x86/GameServer.exe"
echo "  Linux x64:   bin/Release/publish/linux-x64/GameServer"
echo "  macOS x64:   bin/Release/publish/osx-x64/GameServer"
echo "  macOS ARM64: bin/Release/publish/osx-arm64/GameServer"
echo ""
echo "Copy the appropriate executable to your Unity project folder."
echo "Use the GameServerManager.cs script in Unity to launch it."
echo ""

# Make Linux/Mac executables executable
chmod +x "bin/Release/publish/linux-x64/GameServer" 2>/dev/null
chmod +x "bin/Release/publish/osx-x64/GameServer" 2>/dev/null
chmod +x "bin/Release/publish/osx-arm64/GameServer" 2>/dev/null

echo "Linux/Mac executables made executable ✅" 