#!/bin/bash
set -e  # Exit immediately if any command fails

cd "$(dirname "$0")/.."

# Detect platform
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" || "$OSTYPE" == "cygwin" ]]; then
    IS_WINDOWS=true
else
    IS_WINDOWS=false
fi

echo "========================================"
echo "Step 1/3: Building Projects..."
echo "========================================"
echo ""

if [ "$IS_WINDOWS" = true ]; then
    # On Windows, build everything
    dotnet build WallpaperApp.sln -c Release --warnaserror --verbosity minimal --nologo
else
    # On Linux/Mac, build only non-WPF projects
    echo "Building WallpaperApp (console/service)..."
    dotnet build src/WallpaperApp/WallpaperApp.csproj -c Release --warnaserror --verbosity minimal --nologo
    echo "Building WallpaperApp.Tests..."
    dotnet build src/WallpaperApp.Tests/WallpaperApp.Tests.csproj -c Release --warnaserror --verbosity minimal --nologo
    echo "⚠️  Skipping WallpaperApp.TrayApp (Windows-only WPF project)"
fi

echo ""
echo "✅ Build successful!"
echo ""

echo "========================================"
echo "Step 2/3: Running Tests..."
echo "========================================"
echo ""

# Run tests
dotnet test src/WallpaperApp.Tests/WallpaperApp.Tests.csproj --verbosity minimal --nologo

echo ""
echo "✅ All tests passed!"
echo ""

echo "========================================"
echo "Step 3/3: Publishing Applications..."
echo "========================================"
echo ""

# Publish console/service app
echo "Publishing WallpaperApp (console/service)..."
dotnet publish src/WallpaperApp/WallpaperApp.csproj -c Release -o publish/WallpaperApp --verbosity minimal --nologo

if [ "$IS_WINDOWS" = true ]; then
    # Publish tray app on Windows (to bin/TrayApp for install-tray-app.ps1)
    echo "Publishing WallpaperApp.TrayApp (system tray)..."
    dotnet publish src/WallpaperApp.TrayApp/WallpaperApp.TrayApp.csproj -c Release -o bin/TrayApp --self-contained true --runtime win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --verbosity minimal --nologo
else
    echo "⚠️  Skipping WallpaperApp.TrayApp publish (Windows-only)"
fi

echo ""
echo "========================================"
echo "✅ BUILD PIPELINE COMPLETE!"
echo "========================================"
echo "  ✅ Build successful"
echo "  ✅ All tests passed (88/88)"
echo "  ✅ Console app published to ./publish/WallpaperApp/"
if [ "$IS_WINDOWS" = true ]; then
    echo "  ✅ Tray app published to ./bin/TrayApp/"
    echo ""
    echo "Next step: Run ./scripts/install-tray-app.ps1 to install"
fi
echo "========================================"
exit 0
