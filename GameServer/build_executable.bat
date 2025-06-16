@echo off
echo Building GameServer Executable for Unity Integration...
echo.

:: Clean previous builds
if exist "bin\Release\publish" (
    echo Cleaning previous builds...
    rmdir /s /q "bin\Release\publish"
)

:: Build for Windows x64 (self-contained)
echo Building Windows x64 version...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "bin\Release\publish\win-x64"

:: Build for Windows x86 (for older systems)
echo Building Windows x86 version...
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "bin\Release\publish\win-x86"

:: Build for Linux x64 (for cross-platform)
echo Building Linux x64 version...
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "bin\Release\publish\linux-x64"

:: Build for macOS x64
echo Building macOS x64 version...
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "bin\Release\publish\osx-x64"

echo.
echo ✅ Build completed successfully!
echo.
echo Executables created:
echo   Windows x64: bin\Release\publish\win-x64\GameServer.exe
echo   Windows x86: bin\Release\publish\win-x86\GameServer.exe
echo   Linux x64:   bin\Release\publish\linux-x64\GameServer
echo   macOS x64:   bin\Release\publish\osx-x64\GameServer
echo.
echo Copy the appropriate executable to your Unity project folder.
echo Use the GameServerManager.cs script in Unity to launch it.
echo.
pause 