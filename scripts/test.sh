#!/bin/bash
set -e  # Exit immediately if any command fails

echo "========================================"
echo "Running automated tests..."
echo "========================================"
echo ""

cd "$(dirname "$0")/../src"

# Run tests with minimal verbosity for cleaner output
dotnet test --verbosity minimal --nologo

# Check exit code
if [ $? -eq 0 ]; then
    echo ""
    echo "========================================"
    echo "✅ All tests passed!"
    echo "========================================"
    exit 0
else
    echo ""
    echo "========================================"
    echo "❌ Tests failed!"
    echo "========================================"
    exit 1
fi
