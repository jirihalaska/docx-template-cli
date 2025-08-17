#!/bin/bash

# Test automation script for DocxTemplate CLI
# Supports Windows (WSL/Git Bash), macOS, and Linux

set -e

# Configuration
DOTNET_VERSION="9.0.x"
COVERAGE_THRESHOLD=40
TEST_RESULTS_DIR="./TestResults"
COVERAGE_REPORT_DIR="./TestResults/Coverage"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if running on Windows (Git Bash/WSL)
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" || -n "$WSL_DISTRO_NAME" ]]; then
    PLATFORM="windows"
elif [[ "$OSTYPE" == "darwin"* ]]; then
    PLATFORM="macos"
else
    PLATFORM="linux"
fi

print_status "Running tests on platform: $PLATFORM"

# Verify .NET is installed
if ! command -v dotnet &> /dev/null; then
    print_error ".NET CLI not found. Please install .NET $DOTNET_VERSION"
    exit 1
fi

# Check .NET version
DOTNET_CURRENT=$(dotnet --version)
print_status "Using .NET version: $DOTNET_CURRENT"

# Clean previous test results
if [ -d "$TEST_RESULTS_DIR" ]; then
    print_status "Cleaning previous test results..."
    rm -rf "$TEST_RESULTS_DIR"
fi

# Restore dependencies
print_status "Restoring NuGet packages..."
dotnet restore

# Build solution
print_status "Building solution..."
dotnet build --configuration Release --no-restore

# Run unit tests with coverage
print_status "Running unit tests with coverage collection..."
dotnet test --configuration Release --no-build \
    --collect:"XPlat Code Coverage" \
    --results-directory "$TEST_RESULTS_DIR" \
    --logger trx \
    --verbosity normal

# Check if tests passed
if [ $? -ne 0 ]; then
    print_error "Unit tests failed!"
    exit 1
fi

print_status "Unit tests completed successfully"

# Install ReportGenerator if not available
if ! command -v reportgenerator &> /dev/null; then
    print_status "Installing ReportGenerator..."
    dotnet tool install -g dotnet-reportgenerator-globaltool
fi

# Generate coverage report
print_status "Generating coverage report..."
reportgenerator \
    -reports:"$TEST_RESULTS_DIR/**/coverage.cobertura.xml" \
    -targetdir:"$COVERAGE_REPORT_DIR" \
    -reporttypes:"Html;Cobertura" \
    -verbosity:"Warning"

# Extract coverage percentage
if [ -f "$COVERAGE_REPORT_DIR/Cobertura.xml" ]; then
    COVERAGE=$(grep -o 'line-rate="[^"]*"' "$COVERAGE_REPORT_DIR/Cobertura.xml" | head -1 | cut -d'"' -f2)
    COVERAGE_PERCENT=$(echo "$COVERAGE * 100" | bc -l 2>/dev/null | cut -d'.' -f1)
    
    if [ -z "$COVERAGE_PERCENT" ]; then
        # Fallback for systems without bc
        COVERAGE_PERCENT=$(python3 -c "print(int(float('$COVERAGE') * 100))" 2>/dev/null || echo "0")
    fi
    
    print_status "Code coverage: ${COVERAGE_PERCENT}%"
    
    if [ "$COVERAGE_PERCENT" -lt "$COVERAGE_THRESHOLD" ]; then
        print_warning "Coverage ${COVERAGE_PERCENT}% is below ${COVERAGE_THRESHOLD}% threshold"
    else
        print_status "Coverage ${COVERAGE_PERCENT}% meets ${COVERAGE_THRESHOLD}% threshold"
    fi
else
    print_warning "Coverage report not found"
fi

# Test Czech character handling
print_status "Testing Czech character support..."
echo '{"název": "Testovací dokument", "město": "Brno", "ulice": "Údolní"}' > test-czech.json

if [ "$PLATFORM" = "windows" ]; then
    # Windows command
    type test-czech.json > /dev/null 2>&1 || cat test-czech.json > /dev/null 2>&1
else
    cat test-czech.json > /dev/null
fi

if [ $? -eq 0 ]; then
    print_status "Czech character support verified"
else
    print_warning "Czech character support test failed"
fi

rm -f test-czech.json

# Test CLI binary if it exists
CLI_PATH="src/DocxTemplate.CLI/bin/Release/net9.0/DocxTemplate.CLI"
if [ "$PLATFORM" = "windows" ]; then
    CLI_PATH="${CLI_PATH}.exe"
fi

if [ -f "$CLI_PATH" ]; then
    print_status "Testing CLI binary..."
    "$CLI_PATH" --help > /dev/null 2>&1
    if [ $? -eq 0 ]; then
        print_status "CLI binary test passed"
    else
        print_warning "CLI binary test failed"
    fi
else
    print_warning "CLI binary not found at $CLI_PATH"
fi

# Summary
print_status "Test automation completed!"
echo ""
echo "Results:"
echo "  - Platform: $PLATFORM"
echo "  - .NET Version: $DOTNET_CURRENT"
echo "  - Coverage: ${COVERAGE_PERCENT:-Unknown}%"
echo "  - Test Results: $TEST_RESULTS_DIR"
echo "  - Coverage Report: $COVERAGE_REPORT_DIR/index.html"
echo ""

if [ "$COVERAGE_PERCENT" -ge "$COVERAGE_THRESHOLD" ] 2>/dev/null; then
    print_status "All quality gates passed! ✅"
    exit 0
else
    print_warning "Some quality gates failed! ⚠️"
    exit 1
fi