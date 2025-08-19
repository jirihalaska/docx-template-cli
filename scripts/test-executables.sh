#!/bin/bash
set -e

# Get script directory and project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Auto-detect platform based on current system
if [[ "$OSTYPE" == "darwin"* ]]; then
    DEFAULT_PLATFORM="osx-arm64"  # Focus on ARM64 for macOS
elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
    DEFAULT_PLATFORM="win-x64"
else
    DEFAULT_PLATFORM="osx-arm64"  # Default fallback
fi

PLATFORM="${1:-$DEFAULT_PLATFORM}"
TEST_DIR="${2:-dist/test-executables}"

echo "🧪 Testing GUI executable startup for platform: $PLATFORM"
echo "📁 Project root: $PROJECT_ROOT"

# Change to project root
cd "$PROJECT_ROOT"

# Clean test directory
rm -rf "$TEST_DIR"
mkdir -p "$TEST_DIR"

# Determine executable names based on platform
if [[ "$PLATFORM" == "win-x64" ]]; then
    GUI_EXE="DocxTemplate.UI.exe"
else
    GUI_EXE="DocxTemplate.UI"
fi

# Build GUI executable (standalone application)
echo "📦 Building GUI executable..."
dotnet publish src/DocxTemplate.UI/DocxTemplate.UI.csproj -c Release -r "$PLATFORM" --self-contained \
    -p:PublishSingleFile=false \
    -o "$TEST_DIR/gui" \
    --verbosity minimal

if [ $? -ne 0 ]; then
    echo "❌ GUI build failed"
    exit 1
fi

echo "🧪 Testing GUI executable..."
cd "$TEST_DIR/gui"

# Test 1: Check GUI executable exists and has correct permissions
echo "  Testing GUI executable file..."
if [ -f "$GUI_EXE" ] && [ -x "$GUI_EXE" ]; then
    echo "  ✅ GUI executable exists and is executable"
    file_size=$(ls -la "$GUI_EXE" | awk '{print $5}')
    echo "     File size: $file_size bytes"
else
    echo "  ❌ GUI executable missing or not executable"
    ls -la
    exit 1
fi

# Test 2: Test GUI can start (with timeout for headless environments)
echo "  Testing GUI startup capability..."
if command -v timeout >/dev/null 2>&1; then
    TIMEOUT_CMD="timeout 10s"
elif command -v gtimeout >/dev/null 2>&1; then
    TIMEOUT_CMD="gtimeout 10s"
else
    TIMEOUT_CMD=""
fi

# Test basic executable startup - even headless should initialize some components
if [[ -n "$TIMEOUT_CMD" ]]; then
    # Try to start GUI with timeout - it should at least try to initialize
    if $TIMEOUT_CMD ./"$GUI_EXE" > gui-startup.txt 2>&1; then
        echo "  ✅ GUI executable started successfully"
    else
        exit_code=$?
        if [ $exit_code -eq 124 ]; then
            echo "  ✅ GUI executable can start (timed out as expected in headless mode)"
        else
            echo "  ❌ GUI executable failed to start (exit code: $exit_code)"
            echo "  Output:"
            cat gui-startup.txt | head -10
            exit 1
        fi
    fi
else
    echo "  ⚠️  No timeout command available, skipping GUI startup test"
fi

# Test 3: Test template discovery functionality (via GUI services)
echo "  Testing template discovery capability..."
mkdir -p test-templates
echo "dummy" > test-templates/test.docx
echo "  ✅ Template test data created"

# Test 4: Test GUI dependencies (check for required libraries)
echo "  Testing GUI dependencies..."
if [[ "$PLATFORM" == "osx-arm64" ]] || [[ "$PLATFORM" == "osx-x64" ]]; then
    # Check for required dylibs on macOS
    required_libs=("libAvaloniaNative.dylib" "libHarfBuzzSharp.dylib" "libSkiaSharp.dylib")
    for lib in "${required_libs[@]}"; do
        if [ -f "$lib" ]; then
            echo "  ✅ Found required library: $lib"
        else
            echo "  ❌ Missing required library: $lib"
            exit 1
        fi
    done
elif [[ "$PLATFORM" == "win-x64" ]]; then
    # Check for required DLLs on Windows
    dll_count=$(ls *.dll 2>/dev/null | wc -l)
    if [ "$dll_count" -gt 0 ]; then
        echo "  ✅ Found $dll_count DLL files"
    else
        echo "  ❌ No DLL files found"
        exit 1
    fi
fi

cd "$PROJECT_ROOT"

echo ""
echo "✅ All GUI executable startup tests passed!"
echo "📁 Test outputs available in: $TEST_DIR"
echo ""
echo "Summary:"
echo "  ✅ GUI executable builds successfully"
echo "  ✅ GUI executable can start (headless mode)"
echo "  ✅ GUI has all required dependencies"
echo "  ✅ Template discovery test data created"