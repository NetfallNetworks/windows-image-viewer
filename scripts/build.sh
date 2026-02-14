#!/bin/bash
set -e  # Exit immediately if any command fails

echo "========================================"
echo "Building WallpaperApp..."
echo "========================================"
echo ""

cd "$(dirname "$0")/../src/WallpaperApp"

# Build with warnings as errors
dotnet build -c Release --warnaserror

# Check exit code
if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Build successful!"
    exit 0
else
    echo ""
    echo "❌ Build failed!"
    exit 1
fi
