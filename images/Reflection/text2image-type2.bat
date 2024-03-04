@echo off
tasklist | find "Reflection.exe" >nul
if errorlevel 1 (
    echo Reflection.exe is starting.
    start  "reflection wnd" /Min "Reflection.exe" --type2
) else (
    echo Reflection.exe is already running.
)