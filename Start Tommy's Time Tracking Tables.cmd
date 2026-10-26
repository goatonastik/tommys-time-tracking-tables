@echo off
if exist "%~dp0artifacts\release\TommysTimeTrackingTables\TimersApp.exe" (
  start "" "%~dp0artifacts\release\TommysTimeTrackingTables\TimersApp.exe"
  exit /b
)
set "DOTNET_ROOT=%~dp0.tools\dotnet"
start "" "%~dp0src\TimersApp.Desktop\bin\Release\net10.0-windows\TimersApp.exe"
