@echo off
echo 🎮 GameServer - Admin Launcher
echo ===============================
echo.

:: Check if running as administrator
net session >nul 2>&1
if %errorLevel% == 0 (
    echo ✅ Running with administrator privileges
    goto :run_server
) else (
    echo ⚠️ Administrator privileges required for firewall configuration
    echo Requesting elevated privileges...
    
    :: Request administrator privileges and restart
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

:run_server
echo.
echo Starting GameServer with automatic network configuration...
echo.

:: Build the server executable if it doesn't exist
if not exist "bin\Release\publish\win-x64\GameServer.exe" (
    echo Building server executable...
    call build_executable.bat
    if %errorLevel% neq 0 (
        echo ❌ Failed to build server executable
        pause
        exit /b 1
    )
)

:: Run the server with firewall configuration
echo Running server with network configuration...
"bin\Release\publish\win-x64\GameServer.exe" --configure-firewall --show-port-forwarding --port 8080

echo.
echo Server has stopped.
pause 