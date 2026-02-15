#!/bin/bash
set -e  # Exit immediately if any command fails

SCRIPT_DIR="$(dirname "$0")"

echo "========================================"
echo "Weather Wallpaper App - Full Validation"
echo "========================================"
echo ""
echo "Running: Pull -> Test -> Build -> Publish"
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
echo "All steps completed successfully."
echo "Executable: src/WallpaperApp/publish/WallpaperApp.exe"
echo ""
