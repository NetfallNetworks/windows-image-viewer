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
    echo "Building WallpaperApp.Core (shared library)..."
    dotnet build src/WallpaperApp.Core/WallpaperApp.Core.csproj -c Release --warnaserror --verbosity minimal --nologo
    echo "Building WallpaperApp (console)..."
    dotnet build src/WallpaperApp/WallpaperApp.csproj -c Release --warnaserror --verbosity minimal --nologo
    echo "Building WallpaperApp.Tests..."
    dotnet build src/WallpaperApp.Tests/WallpaperApp.Tests.csproj -c Release --warnaserror --verbosity minimal --nologo
    echo "⚠️  Skipping WallpaperApp.TrayApp (Windows-only WPF project)"
    echo "⚠️  Skipping WallpaperApp.WidgetProvider (Windows-only COM server — requires Windows App SDK)"
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
    # Publish tray app on Windows (to bin/TrayApp - source for installer)
    echo "Publishing WallpaperApp.TrayApp (system tray)..."
    dotnet publish src/WallpaperApp.TrayApp/WallpaperApp.TrayApp.csproj -c Release -o bin/TrayApp --self-contained true --runtime win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --verbosity minimal --nologo

    echo ""
    echo "Building installer (MSI)..."
    # Restore WiX v4 from the local tool manifest (.config/dotnet-tools.json)
    if dotnet tool restore >/dev/null 2>&1; then
        dotnet tool run wix extension add WixToolset.UI.wixext/4.0.5 >/dev/null 2>&1 || true
        dotnet tool run wix build installer/Package.wxs \
            -ext WixToolset.UI.wixext \
            -o installer/WallpaperSync-Setup.msi \
            -arch x64
        echo "✅ Installer built: installer/WallpaperSync-Setup.msi"
    else
        echo "⚠️  WiX tool restore failed - skipping installer build"
        echo "   Check .config/dotnet-tools.json and run: dotnet tool restore"
    fi
else
    echo "⚠️  Skipping WallpaperApp.TrayApp publish (Windows-only)"
    echo "⚠️  Skipping full installer build (Windows-only)"
    echo ""

    # Validate WiX installer source schema on Linux.
    # WiX v4 is a cross-platform .NET tool, so we can catch XML schema errors
    # (WIX0005, WIX0400, etc.) here before they surface on Windows.
    # WIX0103 (file not found) is expected on Linux - the TrayApp exe is a
    # Windows-only build artifact and won't exist here. Filter it out.
    echo "Validating WiX installer source schema..."
    if dotnet tool restore >/dev/null 2>&1; then
        dotnet tool run wix extension add WixToolset.UI.wixext/4.0.5 >/dev/null 2>&1 || true
        WIX_OUT=$(dotnet tool run wix build installer/Package.wxs \
            -ext WixToolset.UI.wixext \
            -o /tmp/WallpaperSync-validate.msi \
            -arch x64 2>&1 || true)
        # WIX0103 = source file not found (expected: exe is Windows-only build artifact)
        # WIX0389 = Directory/@Name "not a relative path" - Linux WiX validator false positive
        #           for valid Windows folder names like "WallpaperSync" and "Wallpaper Sync"
        REAL_ERRORS=$(echo "$WIX_OUT" | grep ": error WIX" | grep -v "WIX0103" | grep -v "WIX0389" || true)
        if [ -n "$REAL_ERRORS" ]; then
            echo "❌ WiX installer source has errors - fix before pushing:"
            echo "$REAL_ERRORS"
            exit 1
        fi
        echo "✅ WiX installer source schema OK"
    else
        echo "⚠️  WiX tool restore failed - skipping installer validation"
    fi
fi

echo ""
echo "========================================"
echo "✅ BUILD PIPELINE COMPLETE!"
echo "========================================"
echo "  ✅ Build successful"
echo "  ✅ All tests passed"
echo "  ✅ Console app published to ./publish/WallpaperApp/"
if [ "$IS_WINDOWS" = true ]; then
    echo "  ✅ Tray app published to ./bin/TrayApp/"
    if [ -f "installer/WallpaperSync-Setup.msi" ]; then
        echo "  ✅ Installer built: ./installer/WallpaperSync-Setup.msi"
        echo ""
        echo "Ship installer/WallpaperSync-Setup.msi to end users."
        echo "Double-click to install - no PowerShell or admin rights needed."
    else
        echo "  -- Installer skipped (run: dotnet tool install --global wix)"
    fi
fi
echo "========================================"
exit 0
