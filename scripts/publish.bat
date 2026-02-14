@echo off
echo ========================================
echo Publishing self-contained executable...
echo ========================================
echo.

cd /d "%~dp0\..\src\WallpaperApp"

if exist publish (
    echo Cleaning previous publish directory...
    rmdir /s /q publish
)

dotnet publish -c Release -r win-x64 --self-contained true -o ./publish

if errorlevel 1 (
    echo.
    echo [31mPublish failed![0m
    exit /b 1
) else (
    echo.
    echo [32mPublish successful![0m
    echo.
    echo Output location: src\WallpaperApp\publish\
    echo Executable: src\WallpaperApp\publish\WallpaperApp.exe
    exit /b 0
)
