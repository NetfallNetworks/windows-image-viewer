@echo off
setlocal enabledelayedexpansion

echo ========================================
echo Weather Wallpaper App - Full Validation
echo ========================================
echo.
echo This script will:
echo   1. Pull latest code from git
echo   2. Run all automated tests
echo   3. Build the application
echo   4. Publish self-contained executable
echo.
echo WARNING: Build will stop if any step fails
echo.
pause
echo.

cd /d "%~dp0\.."

REM Step 1: Pull latest code
echo ========================================
echo Step 1/4: Pulling latest code...
echo ========================================
echo.
git pull
if errorlevel 1 (
    echo ERROR: Git pull failed!
    exit /b 1
)
echo.
echo [32mCode updated[0m
echo.

REM Step 2: Run tests
echo ========================================
echo Step 2/4: Running automated tests...
echo ========================================
echo.
cd src
dotnet test --verbosity normal
if errorlevel 1 (
    echo.
    echo [31mTests failed! Fix failing tests before proceeding.[0m
    exit /b 1
)
echo.
echo [32mAll tests passed![0m
echo.

REM Step 3: Build
echo ========================================
echo Step 3/4: Building application...
echo ========================================
echo.
cd WallpaperApp
dotnet build -c Release --warnaserror
if errorlevel 1 (
    echo.
    echo [31mBuild failed![0m
    exit /b 1
)
echo.
echo [32mBuild successful![0m
echo.

REM Step 4: Publish
echo ========================================
echo Step 4/4: Publishing executable...
echo ========================================
echo.
if exist publish (
    echo Cleaning previous publish directory...
    rmdir /s /q publish
)
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
if errorlevel 1 (
    echo.
    echo [31mPublish failed![0m
    exit /b 1
)
echo.
echo [32mPublish successful![0m
echo.
echo Output location: src\WallpaperApp\publish\
echo Executable: src\WallpaperApp\publish\WallpaperApp.exe
echo.

REM Success summary
echo ========================================
echo [32mVALIDATION COMPLETE![0m
echo ========================================
echo.
echo All automated checks passed. Next steps:
echo.
echo   Manual Testing:
echo     1. Run: src\WallpaperApp\publish\WallpaperApp.exe
echo     2. Verify console output shows startup message
echo     3. Check application exits cleanly
echo.
set /p answer="Ready for manual validation? (y/n): "
if /i "%answer%"=="y" (
    echo.
    echo Starting executable...
    cd publish
    WallpaperApp.exe
    echo.
    echo Application exited with code: !errorlevel!
)

echo.
echo All done!
pause
