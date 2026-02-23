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
echo "Step 1/6: Building Projects..."
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
echo "Step 2/6: Running Tests..."
echo "========================================"
echo ""

# Run tests
dotnet test src/WallpaperApp.Tests/WallpaperApp.Tests.csproj --verbosity minimal --nologo

echo ""
echo "✅ All tests passed!"
echo ""

echo "========================================"
echo "Step 3/6: Publishing Applications..."
echo "========================================"
echo ""

# Publish console/service app
echo "Publishing WallpaperApp (console/service)..."
dotnet publish src/WallpaperApp/WallpaperApp.csproj -c Release -o publish/WallpaperApp --verbosity minimal --nologo

if [ "$IS_WINDOWS" = true ]; then
    # Publish tray app on Windows (to bin/TrayApp - source for installer)
    echo "Publishing WallpaperApp.TrayApp (system tray)..."
    dotnet publish src/WallpaperApp.TrayApp/WallpaperApp.TrayApp.csproj -c Release -o bin/TrayApp --self-contained true --runtime win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --verbosity minimal --nologo
else
    echo "⚠️  Skipping WallpaperApp.TrayApp publish (Windows-only)"
fi

echo ""

echo "========================================"
echo "Step 4/6: Publishing Widget Provider..."
echo "========================================"
echo ""

if [ "$IS_WINDOWS" = true ]; then
    # Clean WidgetProvider intermediates to avoid stale WinRT activation manifest
    rm -rf src/WallpaperApp.WidgetProvider/obj src/WallpaperApp.WidgetProvider/bin
    echo "Publishing WallpaperApp.WidgetProvider (widget COM server)..."
    # NOT PublishSingleFile: Windows App SDK generates SxS-incompatible manifest with SingleFile
    dotnet publish src/WallpaperApp.WidgetProvider/WallpaperApp.WidgetProvider.csproj -c Release -o bin/WidgetProvider --self-contained true --runtime win-x64 --verbosity minimal --nologo
    echo "✅ Widget provider published"
else
    echo "[SKIPPED on Linux] Step 4: Widget provider (Windows App SDK required)"
fi

echo ""

echo "========================================"
echo "Step 5/6: Building Identity MSIX..."
echo "========================================"
echo ""

if [ "$IS_WINDOWS" = true ]; then
    echo "Building identity MSIX package..."
    if powershell -ExecutionPolicy Bypass -File installer/IdentityPackage/build-identity-package.ps1; then
        echo "✅ Identity MSIX built: installer/WallpaperSync-Identity.msix"
    else
        echo "⚠️  Identity MSIX build failed (requires Windows SDK: makeappx.exe, signtool.exe)"
    fi
else
    echo "[SKIPPED on Linux] Step 5: Identity MSIX (makeappx.exe Windows-only)"
fi

echo ""

echo "========================================"
echo "Step 6/6: Building Installer (MSI)..."
echo "========================================"
echo ""

if [ "$IS_WINDOWS" = true ]; then
    echo "Building installer (MSI)..."
    # Restore WiX v4 from the local tool manifest (.config/dotnet-tools.json)
    if dotnet tool restore >/dev/null 2>&1; then
        dotnet tool run wix extension add WixToolset.UI.wixext/4.0.5 >/dev/null 2>&1 || true

        # Include Widget feature only when both artifacts exist (Steps 4+5 succeeded)
        WIX_WIDGET_FLAG=""
        WIX_WIDGET_FILES=""
        if [ -f "bin/WidgetProvider/WallpaperApp.WidgetProvider.exe" ] && \
           [ -f "installer/WallpaperSync-Identity.msix" ]; then
            # Harvest widget provider files into a WiX fragment
            echo "Harvesting widget provider files..."
            powershell -ExecutionPolicy Bypass -File installer/harvest-widget-files.ps1
            WIX_WIDGET_FLAG="-d IncludeWidget=true"
            WIX_WIDGET_FILES="installer/WidgetProviderFiles.wxs"
            echo "Including Widget Board feature in installer."
        else
            echo "Excluding Widget Board feature (missing artifacts)."
        fi

        dotnet tool run wix build installer/Package.wxs $WIX_WIDGET_FILES \
            -ext WixToolset.UI.wixext \
            -o installer/WallpaperSync-Setup.msi \
            -arch x64 $WIX_WIDGET_FLAG
        echo "✅ Installer built: installer/WallpaperSync-Setup.msi"
    else
        echo "⚠️  WiX tool restore failed - skipping installer build"
        echo "   Check .config/dotnet-tools.json and run: dotnet tool restore"
    fi
else
    echo "[SKIPPED on Linux] Step 6: MSI installer (Windows-only)"
    echo ""

    # Validate WiX installer source schema on Linux.
    # WiX v4 is a cross-platform .NET tool, so we can catch XML schema errors
    # (WIX0005, WIX0400, etc.) here before they surface on Windows.
    # WIX0103 (file not found) is expected on Linux - the build artifacts are
    # Windows-only and won't exist here. Filter it out.
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
    if [ -f "bin/WidgetProvider/WallpaperApp.WidgetProvider.exe" ]; then
        echo "  ✅ Widget provider published to ./bin/WidgetProvider/"
    else
        echo "  -- Widget provider skipped"
    fi
    if [ -f "installer/WallpaperSync-Identity.msix" ]; then
        echo "  ✅ Identity package built: ./installer/WallpaperSync-Identity.msix"
    else
        echo "  -- Identity package skipped (requires Windows SDK)"
    fi
    if [ -f "installer/WallpaperSync-Setup.msi" ]; then
        echo "  ✅ Installer built: ./installer/WallpaperSync-Setup.msi"
        echo ""
        echo "Ship installer/WallpaperSync-Setup.msi to end users."
        echo "Double-click to install - no PowerShell or admin rights needed."
    else
        echo "  -- Installer skipped (run: dotnet tool install --global wix)"
    fi
else
    echo "  [SKIPPED on Linux] Tray app publish (Windows-only)"
    echo "  [SKIPPED on Linux] Widget provider publish (Windows App SDK required)"
    echo "  [SKIPPED on Linux] Identity MSIX build (makeappx.exe Windows-only)"
    echo "  [SKIPPED on Linux] MSI installer build (Windows-only)"
fi
echo "========================================"
exit 0
