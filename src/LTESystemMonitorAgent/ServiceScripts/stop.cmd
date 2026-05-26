@echo off
setlocal

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0stop-service.ps1"
exit /b %ERRORLEVEL%
