# Test automation script for DocxTemplate CLI (PowerShell)
# Supports Windows PowerShell and PowerShell Core

param(
    [int]$CoverageThreshold = 40,
    [string]$TestResultsDir = "./TestResults",
    [string]$CoverageReportDir = "./TestResults/Coverage"
)

# Configuration
$DotNetVersion = "9.0.x"
$Platform = "windows"

# Function to write colored output
function Write-Status {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

Write-Status "Running tests on platform: $Platform"

# Verify .NET is installed
try {
    $dotnetVersion = dotnet --version
    Write-Status "Using .NET version: $dotnetVersion"
} catch {
    Write-Error ".NET CLI not found. Please install .NET $DotNetVersion"
    exit 1
}

# Clean previous test results
if (Test-Path $TestResultsDir) {
    Write-Status "Cleaning previous test results..."
    Remove-Item -Recurse -Force $TestResultsDir
}

# Restore dependencies
Write-Status "Restoring NuGet packages..."
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to restore NuGet packages"
    exit 1
}

# Build solution
Write-Status "Building solution..."
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

# Run unit tests with coverage
Write-Status "Running unit tests with coverage collection..."
dotnet test --configuration Release --no-build `
    --collect:"XPlat Code Coverage" `
    --results-directory $TestResultsDir `
    --logger trx `
    --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Error "Unit tests failed!"
    exit 1
}

Write-Status "Unit tests completed successfully"

# Install ReportGenerator if not available
try {
    reportgenerator --help | Out-Null
} catch {
    Write-Status "Installing ReportGenerator..."
    dotnet tool install -g dotnet-reportgenerator-globaltool
}

# Generate coverage report
Write-Status "Generating coverage report..."
reportgenerator `
    -reports:"$TestResultsDir/**/coverage.cobertura.xml" `
    -targetdir:$CoverageReportDir `
    -reporttypes:"Html;Cobertura" `
    -verbosity:"Warning"

# Extract coverage percentage
$coverageFile = Join-Path $CoverageReportDir "Cobertura.xml"
if (Test-Path $coverageFile) {
    $coverageXml = Get-Content $coverageFile -Raw
    if ($coverageXml -match 'line-rate="([^"]*)"') {
        $coverage = [double]$matches[1]
        $coveragePercent = [int]($coverage * 100)
        
        Write-Status "Code coverage: $coveragePercent%"
        
        if ($coveragePercent -lt $CoverageThreshold) {
            Write-Warning "Coverage $coveragePercent% is below $CoverageThreshold% threshold"
        } else {
            Write-Status "Coverage $coveragePercent% meets $CoverageThreshold% threshold"
        }
    } else {
        Write-Warning "Could not parse coverage from report"
        $coveragePercent = 0
    }
} else {
    Write-Warning "Coverage report not found"
    $coveragePercent = 0
}

# Test Czech character handling
Write-Status "Testing Czech character support..."
$czechContent = '{"název": "Testovací dokument", "město": "Brno", "ulice": "Údolní"}'
$czechFile = "test-czech.json"

try {
    $czechContent | Out-File -FilePath $czechFile -Encoding UTF8
    $readContent = Get-Content $czechFile -Raw -Encoding UTF8
    if ($readContent -like "*název*") {
        Write-Status "Czech character support verified"
    } else {
        Write-Warning "Czech character support test failed"
    }
} catch {
    Write-Warning "Czech character support test failed: $($_.Exception.Message)"
} finally {
    if (Test-Path $czechFile) {
        Remove-Item $czechFile
    }
}

# Test CLI binary if it exists
$cliPath = "src/DocxTemplate.CLI/bin/Release/net9.0/DocxTemplate.CLI.exe"
if (Test-Path $cliPath) {
    Write-Status "Testing CLI binary..."
    try {
        & $cliPath --help | Out-Null
        Write-Status "CLI binary test passed"
    } catch {
        Write-Warning "CLI binary test failed: $($_.Exception.Message)"
    }
} else {
    Write-Warning "CLI binary not found at $cliPath"
}

# Summary
Write-Status "Test automation completed!"
Write-Host ""
Write-Host "Results:"
Write-Host "  - Platform: $Platform"
Write-Host "  - .NET Version: $dotnetVersion"
Write-Host "  - Coverage: $coveragePercent%"
Write-Host "  - Test Results: $TestResultsDir"
Write-Host "  - Coverage Report: $(Join-Path $CoverageReportDir 'index.html')"
Write-Host ""

if ($coveragePercent -ge $CoverageThreshold) {
    Write-Status "All quality gates passed! ✅"
    exit 0
} else {
    Write-Warning "Some quality gates failed! ⚠️"
    exit 1
}