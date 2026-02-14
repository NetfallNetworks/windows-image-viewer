#!/bin/bash
set -e  # Exit immediately if any command fails

SCRIPT_DIR="$(dirname "$0")"

echo "========================================"
echo "Weather Wallpaper App - Full Validation"
echo "========================================"
echo ""
echo "This script will:"
echo "  1. Pull latest code from git"
echo "  2. Run all automated tests"
echo "  3. Build the application"
echo "  4. Publish self-contained executable"
echo ""
echo "⚠️  Build will stop if any step fails"
echo ""
read -p "Press Enter to continue..."
echo ""

# Step 1: Pull latest code
echo "========================================"
echo "Step 1/4: Pulling latest code..."
echo "========================================"
echo ""
cd "$(dirname "$0")/.."
git pull
echo ""
echo "✅ Code updated"
echo ""

# Step 2: Run tests
echo "========================================"
echo "Step 2/4: Running automated tests..."
echo "========================================"
echo ""
bash "$SCRIPT_DIR/test.sh"
echo ""

# Step 3: Build
echo "========================================"
echo "Step 3/4: Building application..."
echo "========================================"
echo ""
bash "$SCRIPT_DIR/build.sh"
echo ""

# Step 4: Publish
echo "========================================"
echo "Step 4/4: Publishing executable..."
echo "========================================"
echo ""
bash "$SCRIPT_DIR/publish.sh"
echo ""

# Success summary
echo "========================================"
echo "✅ VALIDATION COMPLETE!"
echo "========================================"
echo ""
echo "All automated checks passed. Next steps:"
echo ""
echo "  Manual Testing:"
echo "    1. Run: src/WallpaperApp/publish/WallpaperApp.exe"
echo "    2. Verify console output shows startup message"
echo "    3. Check exit code is 0"
echo ""
echo "Ready for manual validation? (y/n)"
read -p "> " answer
if [ "$answer" = "y" ] || [ "$answer" = "Y" ]; then
    echo ""
    echo "Starting manual test instructions..."
    echo ""
    echo "Run these commands in a Windows environment:"
    echo ""
    echo "  cd src/WallpaperApp/publish"
    echo "  .\\WallpaperApp.exe"
    echo ""
    echo "Expected output:"
    echo "  Weather Wallpaper App - Starting..."
    echo ""
    echo "Press any key when manual testing is complete..."
    read -n 1
fi

echo ""
echo "✅ All done!"
