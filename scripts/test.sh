#!/bin/bash
set -e  # Exit immediately if any command fails

echo "========================================"
echo "Running automated tests..."
echo "========================================"
echo ""

cd "$(dirname "$0")/../src"

# Run tests with detailed output
dotnet test --verbosity normal

# Check exit code
if [ $? -eq 0 ]; then
    echo ""
    echo "✅ All tests passed!"
    exit 0
else
    echo ""
    echo "❌ Tests failed! Fix failing tests before proceeding."
    exit 1
fi
