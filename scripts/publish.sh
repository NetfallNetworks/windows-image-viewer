#!/bin/bash
set -e  # Exit immediately if any command fails

echo "========================================"
echo "Publishing self-contained executable..."
echo "========================================"
echo ""

cd "$(dirname "$0")/../src/WallpaperApp"

# Clean previous publish output
if [ -d "./publish" ]; then
    echo "Cleaning previous publish directory..."
    rm -rf ./publish
fi

# Publish self-contained for Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish

# Check exit code
if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Publish successful!"
    echo ""
    echo "Output location: src/WallpaperApp/publish/"
    echo "Executable: src/WallpaperApp/publish/WallpaperApp.exe"
    exit 0
else
    echo ""
    echo "❌ Publish failed!"
    exit 1
fi
