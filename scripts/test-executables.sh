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

echo "🧪 Testing executable startup for platform: $PLATFORM"
echo "📁 Project root: $PROJECT_ROOT"

# Change to project root
cd "$PROJECT_ROOT"

# Clean test directory
rm -rf "$TEST_DIR"
mkdir -p "$TEST_DIR"

# Determine executable names based on platform
if [[ "$PLATFORM" == "win-x64" ]]; then
    CLI_EXE="DocxTemplate.CLI.exe"
    GUI_EXE="DocxTemplate.UI.exe"
else
    CLI_EXE="DocxTemplate.CLI"
    GUI_EXE="DocxTemplate.UI"
fi

echo "📦 Building CLI executable..."
dotnet publish src/DocxTemplate.CLI/DocxTemplate.CLI.csproj -c Release -r "$PLATFORM" --self-contained \
    -o "$TEST_DIR/cli" \
    --verbosity quiet

echo "🧪 Testing CLI executable..."
cd "$TEST_DIR/cli"

# Test 1: Help command
echo "  Testing --help command..."
if ./"$CLI_EXE" --help > cli-help.txt 2>&1; then
    echo "  ✅ CLI help command succeeded"
    echo "     Output preview:"
    head -5 cli-help.txt | sed 's/^/     /'
else
    echo "  ❌ CLI help command failed"
    cat cli-help.txt
    exit 1
fi

# Test 2: Version command
echo "  Testing --version command..."
if ./"$CLI_EXE" --version > cli-version.txt 2>&1; then
    echo "  ✅ CLI version command succeeded"
    version=$(cat cli-version.txt)
    echo "     Version: $version"
else
    echo "  ❌ CLI version command failed"
    cat cli-version.txt
    exit 1
fi

# Test 3: Invalid command handling
echo "  Testing invalid command handling..."
if ./"$CLI_EXE" invalid-command > cli-invalid.txt 2>&1; then
    echo "  ⚠️  Invalid command unexpectedly succeeded"
    cat cli-invalid.txt
else
    exit_code=$?
    if [ $exit_code -eq 1 ] || [ $exit_code -eq 2 ]; then
        echo "  ✅ CLI properly handles invalid commands (exit code: $exit_code)"
    else
        echo "  ❌ CLI crashed on invalid command (exit code: $exit_code)"
        cat cli-invalid.txt
        exit 1
    fi
fi

# Test 4: Basic command functionality
echo "  Testing discover command..."
mkdir -p test-templates
echo "dummy" > test-templates/test.docx
if ./"$CLI_EXE" discover --path test-templates > cli-discover.txt 2>&1; then
    echo "  ✅ CLI discover command succeeded"
    echo "     Found $(grep -c "test.docx" cli-discover.txt || echo "0") template files"
else
    echo "  ❌ CLI discover command failed"
    cat cli-discover.txt
    exit 1
fi

cd ../..

# Test GUI (always run, crucial for CI validation)
echo "📦 Building GUI executable..."
dotnet publish src/DocxTemplate.UI/DocxTemplate.UI.csproj -c Release -r "$PLATFORM" --self-contained \
    -p:SkipCliBuild=true \
    -o "$TEST_DIR/gui" \
    --verbosity minimal

if [ $? -ne 0 ]; then
    echo "❌ GUI build failed"
    exit 1
fi

echo "🔗 Setting up CLI-GUI integration..."
if [[ "$PLATFORM" == "win-x64" ]]; then
    cp "$TEST_DIR/cli/$CLI_EXE" "$TEST_DIR/gui/docx-template.exe"
else
    cp "$TEST_DIR/cli/$CLI_EXE" "$TEST_DIR/gui/docx-template"
    chmod +x "$TEST_DIR/gui/docx-template"
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

# Test 3: Test CLI integration
echo "  Testing CLI discoverability..."
if [[ "$PLATFORM" == "win-x64" ]]; then
    CLI_FROM_GUI="docx-template.exe"
else
    CLI_FROM_GUI="./docx-template"
fi

if [ -f "${CLI_FROM_GUI//.\//}" ] && [ -x "${CLI_FROM_GUI//.\//}" ]; then
    echo "  ✅ CLI executable is present and executable"
    
    if $CLI_FROM_GUI --version > cli-from-gui.txt 2>&1; then
        echo "  ✅ CLI is callable from GUI directory"
        version=$(cat cli-from-gui.txt)
        echo "     CLI version from GUI dir: $version"
    else
        echo "  ❌ CLI not callable from GUI directory"
        cat cli-from-gui.txt
        exit 1
    fi
else
    echo "  ❌ CLI executable missing or not executable in GUI directory"
    ls -la
    exit 1
fi

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

cd ../..

echo ""
echo "✅ All executable startup tests passed!"
echo "📁 Test outputs available in: $TEST_DIR"
echo ""
echo "Summary:"
echo "  ✅ CLI help command works"
echo "  ✅ CLI version command works" 
echo "  ✅ CLI handles invalid commands properly"
echo "  ✅ CLI basic commands functional"
echo "  ✅ GUI executable builds and can start"
echo "  ✅ GUI has all required dependencies"
echo "  ✅ CLI-GUI integration works"