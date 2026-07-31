@echo off
setlocal

set "PROJECT=%~dp0pc-software\src\DigitalKeyDisplay\DigitalKeyDisplay.csproj"

if not exist "%PROJECT%" (
    echo Digital key project file was not found.
    pause
    exit /b 1
)

where dotnet >nul 2>nul
if errorlevel 1 (
    echo .NET SDK was not found. Install .NET 7 SDK first.
    pause
    exit /b 1
)

start "" /D "%~dp0" dotnet run --project "%PROJECT%" -c Release
exit /b 0
