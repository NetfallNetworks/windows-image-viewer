@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."

echo ========================================
echo Weather Wallpaper App - Full Validation
echo ========================================
echo.
echo Running: Pull -^> Test -^> Build -^> Publish
echo.

REM Step 1: Pull latest code
echo ========================================
echo Step 1/4: Pulling latest code...
echo ========================================
echo.

cd /d "%REPO_ROOT%"
git pull
if errorlevel 1 (
    echo.
    echo ERROR: Git pull failed!
    pause
    exit /b 1
)
echo.
echo SUCCESS: Code updated
echo.

REM Step 2: Run tests
echo ========================================
echo Step 2/4: Running automated tests...
echo ========================================
echo.

cd /d "%REPO_ROOT%\src"
dotnet test WallpaperApp.Tests/WallpaperApp.Tests.csproj --verbosity minimal --nologo
if errorlevel 1 (
    echo.
    echo ERROR: Tests failed!
    pause
    exit /b 1
)
echo.
echo SUCCESS: All tests passed!
echo.

REM Step 3: Build
echo ========================================
echo Step 3/4: Building application...
echo ========================================
echo.

cd /d "%REPO_ROOT%\src\WallpaperApp"
dotnet build -c Release --warnaserror --verbosity minimal --nologo
if errorlevel 1 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)
echo.
echo SUCCESS: Build successful!
echo.

REM Step 4: Publish
echo ========================================
echo Step 4/4: Publishing executable...
echo ========================================
echo.

if exist "%REPO_ROOT%\src\WallpaperApp\publish" (
    echo Cleaning previous publish directory...
    rmdir /s /q "%REPO_ROOT%\src\WallpaperApp\publish"
)

cd /d "%REPO_ROOT%\src\WallpaperApp"
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish --verbosity minimal --nologo
if errorlevel 1 (
    echo.
    echo ERROR: Publish failed!
    pause
    exit /b 1
)
echo.
echo SUCCESS: Publish successful!
echo.

REM Success summary
echo ========================================
echo VALIDATION COMPLETE!
echo ========================================
echo.
echo All steps completed successfully.
echo Executable: src\WallpaperApp\publish\WallpaperApp.exe
echo.
pause
exit /b 0
