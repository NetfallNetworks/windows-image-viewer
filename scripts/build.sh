#!/bin/bash
set -e  # Exit immediately if any command fails

echo "========================================"
echo "Building WallpaperApp..."
echo "========================================"
echo ""

cd "$(dirname "$0")/../src/WallpaperApp"

# Build with warnings as errors, minimal verbosity
dotnet build -c Release --warnaserror --verbosity minimal --nologo

echo ""
echo "========================================"
echo "✅ Build successful!"
echo "========================================"
exit 0
