@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."
set "LOG_FILE=%REPO_ROOT%\test-output.log"

echo ======================================== > "%LOG_FILE%"
echo Running automated tests... >> "%LOG_FILE%"
echo Started at %date% %time% >> "%LOG_FILE%"
echo ======================================== >> "%LOG_FILE%"
echo. >> "%LOG_FILE%"

echo ========================================
echo Running automated tests...
echo ========================================
echo.
echo Working directory: %REPO_ROOT%\src
echo Output will be saved to: test-output.log
echo.

cd /d "%REPO_ROOT%\src"
dotnet test --verbosity normal >> "%LOG_FILE%" 2>&1

if errorlevel 1 (
    echo.
    echo ERROR: Tests failed! Fix failing tests before proceeding.
    echo.
    echo Full output saved to: %LOG_FILE%
    pause
    exit /b 1
) else (
    echo.
    echo SUCCESS: All tests passed!
    echo.
    echo Full output saved to: %LOG_FILE%
    pause
    exit /b 0
)
