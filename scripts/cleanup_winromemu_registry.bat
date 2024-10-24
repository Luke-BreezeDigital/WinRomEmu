@echo off
title WinRomEmu Registry Cleanup

:: Check for admin rights
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running with administrator privileges...
) else (
    echo Please run this script as administrator
    echo Right-click the script and select "Run as administrator"
    pause
    exit /b 1
)

echo.
echo Starting registry cleanup for WinRomEmu...
echo.

:: Current WinRomEmu keys
REG DELETE "HKCU\Software\Classes\*\shell\WinRomEmu" /f
REG DELETE "HKCU\Software\Classes\*\shell\WinRomEmuDefault" /f
REG DELETE "HKCU\Software\Classes\Directory\shell\WinRomEmu" /f

:: Legacy EmulatorManager keys
REG DELETE "HKCU\Software\Classes\*\shell\EmulatorManager" /f
REG DELETE "HKCU\Software\Classes\*\shell\EmulatorManagerDefault" /f
REG DELETE "HKCU\Software\Classes\Directory\shell\EmulatorManager" /f

:: Additional possible variations
REG DELETE "HKCU\Directory\shell\WinRomEmu" /f
REG DELETE "HKCU\Directory\shell\EmulatorManager" /f

echo.
echo Registry cleanup complete!
echo.
echo Would you like to restart Windows Explorer to ensure all changes take effect?
choice /C YN /M "Restart Explorer now"
if errorlevel 2 goto :skip_restart
if errorlevel 1 goto :do_restart

:do_restart
echo.
echo Restarting Explorer...
taskkill /f /im explorer.exe >nul 2>&1
start explorer.exe
echo Explorer has been restarted.
goto :end

:skip_restart
echo.
echo Explorer restart skipped. Some changes may require a manual restart or logoff.

:end
echo.
echo Cleanup process complete.
pause