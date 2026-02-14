@echo off
setlocal enabledelayedexpansion

REM Setup output logging
set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."
set "LOG_FILE=%REPO_ROOT%\validate-output.log"

REM Start logging
echo Validation started at %date% %time% > "%LOG_FILE%"
echo. >> "%LOG_FILE%"

REM Function to log and display
call :log "========================================"
call :log "Weather Wallpaper App - Full Validation"
call :log "========================================"
call :log ""
call :log "This script will:"
call :log "  1. Pull latest code from git"
call :log "  2. Run all automated tests"
call :log "  3. Build the application"
call :log "  4. Publish self-contained executable"
call :log ""
call :log "WARNING: Build will stop if any step fails"
call :log "Output will be saved to: validate-output.log"
call :log ""
pause
echo. >> "%LOG_FILE%"

REM Step 1: Pull latest code
call :log "========================================"
call :log "Step 1/4: Pulling latest code..."
call :log "========================================"
call :log ""

cd /d "%REPO_ROOT%"
git pull >> "%LOG_FILE%" 2>&1
if errorlevel 1 (
    call :log "ERROR: Git pull failed!"
    call :log "See %LOG_FILE% for details"
    pause
    exit /b 1
)
call :log ""
call :log "SUCCESS: Code updated"
call :log ""

REM Step 2: Run tests
call :log "========================================"
call :log "Step 2/4: Running automated tests..."
call :log "========================================"
call :log ""
call :log "Working directory: %REPO_ROOT%\src"

cd /d "%REPO_ROOT%\src"
dotnet test --verbosity normal >> "%LOG_FILE%" 2>&1
if errorlevel 1 (
    call :log ""
    call :log "ERROR: Tests failed! Fix failing tests before proceeding."
    call :log "See %LOG_FILE% for details"
    pause
    exit /b 1
)
call :log ""
call :log "SUCCESS: All tests passed!"
call :log ""

REM Step 3: Build
call :log "========================================"
call :log "Step 3/4: Building application..."
call :log "========================================"
call :log ""
call :log "Working directory: %REPO_ROOT%\src\WallpaperApp"

cd /d "%REPO_ROOT%\src\WallpaperApp"
dotnet build -c Release --warnaserror >> "%LOG_FILE%" 2>&1
if errorlevel 1 (
    call :log ""
    call :log "ERROR: Build failed!"
    call :log "See %LOG_FILE% for details"
    pause
    exit /b 1
)
call :log ""
call :log "SUCCESS: Build successful!"
call :log ""

REM Step 4: Publish
call :log "========================================"
call :log "Step 4/4: Publishing executable..."
call :log "========================================"
call :log ""

if exist "%REPO_ROOT%\src\WallpaperApp\publish" (
    call :log "Cleaning previous publish directory..."
    rmdir /s /q "%REPO_ROOT%\src\WallpaperApp\publish" >> "%LOG_FILE%" 2>&1
)

cd /d "%REPO_ROOT%\src\WallpaperApp"
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish >> "%LOG_FILE%" 2>&1
if errorlevel 1 (
    call :log ""
    call :log "ERROR: Publish failed!"
    call :log "See %LOG_FILE% for details"
    pause
    exit /b 1
)
call :log ""
call :log "SUCCESS: Publish successful!"
call :log ""
call :log "Output location: src\WallpaperApp\publish\"
call :log "Executable: src\WallpaperApp\publish\WallpaperApp.exe"
call :log ""

REM Success summary
call :log "========================================"
call :log "VALIDATION COMPLETE!"
call :log "========================================"
call :log ""
call :log "All automated checks passed. Next steps:"
call :log ""
call :log "  Manual Testing:"
call :log "    1. Run: src\WallpaperApp\publish\WallpaperApp.exe"
call :log "    2. Verify console output shows startup message"
call :log "    3. Check application exits cleanly"
call :log ""

set /p answer="Ready for manual validation? (y/n): "
if /i "%answer%"=="y" (
    echo.
    echo Starting executable...
    echo. >> "%LOG_FILE%"
    echo Starting manual test... >> "%LOG_FILE%"
    cd /d "%REPO_ROOT%\src\WallpaperApp\publish"
    WallpaperApp.exe
    echo.
    echo Application exited with code: !errorlevel!
    echo Application exited with code: !errorlevel! >> "%LOG_FILE%"
)

echo.
echo All done!
echo Full output saved to: %LOG_FILE%
pause
exit /b 0

REM Function to log and display messages
:log
set "msg=%~1"
echo %msg%
echo %msg% >> "%LOG_FILE%"
goto :eof
