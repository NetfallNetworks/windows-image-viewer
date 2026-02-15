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
call :log "Running: Pull -> Test -> Build -> Publish"
call :log "Output will be saved to: validate-output.log"
call :log ""

REM Step 1: Pull latest code
call :log "========================================"
call :log "Step 1/4: Pulling latest code..."
call :log "========================================"
call :log ""

cd /d "%REPO_ROOT%"
git pull >> "%LOG_FILE%" 2>&1
if errorlevel 1 (
    call :log "ERROR: Git pull failed! See %LOG_FILE% for details"
    exit /b 1
)
call :log "SUCCESS: Code updated"
call :log ""

REM Step 2: Run tests
call :log "========================================"
call :log "Step 2/4: Running automated tests..."
call :log "========================================"
call :log ""

cd /d "%REPO_ROOT%\src"
dotnet test WallpaperApp.Tests/WallpaperApp.Tests.csproj --verbosity normal >> "%LOG_FILE%" 2>&1
if errorlevel 1 (
    call :log "ERROR: Tests failed! See %LOG_FILE% for details"
    exit /b 1
)
call :log "SUCCESS: All tests passed!"
call :log ""

REM Step 3: Build
call :log "========================================"
call :log "Step 3/4: Building application..."
call :log "========================================"
call :log ""

cd /d "%REPO_ROOT%\src\WallpaperApp"
dotnet build -c Release --warnaserror >> "%LOG_FILE%" 2>&1
if errorlevel 1 (
    call :log "ERROR: Build failed! See %LOG_FILE% for details"
    exit /b 1
)
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
    call :log "ERROR: Publish failed! See %LOG_FILE% for details"
    exit /b 1
)
call :log "SUCCESS: Publish successful!"
call :log ""

REM Success summary
call :log "========================================"
call :log "VALIDATION COMPLETE!"
call :log "========================================"
call :log ""
call :log "All steps completed successfully."
call :log "Executable: src\WallpaperApp\publish\WallpaperApp.exe"
call :log "Full output saved to: %LOG_FILE%"
call :log ""
exit /b 0

REM Function to log and display messages
:log
set "msg=%~1"
if "%msg%"=="" (
    echo.
    echo. >> "%LOG_FILE%"
) else (
    echo %msg%
    echo %msg% >> "%LOG_FILE%"
)
goto :eof
