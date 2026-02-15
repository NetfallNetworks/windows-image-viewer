#!/bin/bash
set -e  # Exit immediately if any command fails

echo "========================================"
echo "Building All Projects..."
echo "========================================"
echo ""

cd "$(dirname "$0")/.."

# Build all projects using the solution file
dotnet build WallpaperApp.sln -c Release --warnaserror --verbosity minimal --nologo

echo ""
echo "========================================"
echo "✅ Build successful!"
echo "  - WallpaperApp (console/service)"
echo "  - WallpaperApp.TrayApp (system tray)"
echo "  - WallpaperApp.Tests"
echo "========================================"
exit 0
