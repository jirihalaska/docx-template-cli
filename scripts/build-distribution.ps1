#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "./dist",
    [string]$Version = "",
    [switch]$SkipTests = $false
)

$ErrorActionPreference = "Stop"

# Get version from git if not provided
if ([string]::IsNullOrEmpty($Version)) {
    try {
        $Version = git describe --tags --abbrev=0 2>$null
        if ([string]::IsNullOrEmpty($Version)) {
            $Version = "0.1.0"
        }
    } catch {
        $Version = "0.1.0"
    }
}

Write-Host "Building DocxTemplate Distribution Package v$Version" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Output Path: $OutputPath" -ForegroundColor Yellow

# Clean and prepare output directory
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

# Run tests unless skipped
if (-not $SkipTests) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test --configuration $Configuration --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed"
    }
}

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    throw "Build failed"
}

# Detect platform
$RuntimeId = ""
if ($IsWindows -or $env:OS -eq "Windows_NT") {
    $RuntimeId = "win-x64"
    $GuiExecutableName = "DocxTemplate.UI.exe"
    $CliExecutableName = "docx-template.exe"
} elseif ($IsMacOS -or (uname 2>$null) -eq "Darwin") {
    $RuntimeId = "osx-x64"
    $GuiExecutableName = "DocxTemplate.UI"
    $CliExecutableName = "docx-template"
} elseif ($IsLinux -or (uname 2>$null) -eq "Linux") {
    $RuntimeId = "linux-x64"
    $GuiExecutableName = "DocxTemplate.UI"
    $CliExecutableName = "docx-template"
} else {
    throw "Unsupported platform"
}

Write-Host "Building for platform: $RuntimeId" -ForegroundColor Yellow

# Create platform-specific output directory
$PlatformOutputPath = Join-Path $OutputPath $RuntimeId
New-Item -ItemType Directory -Path $PlatformOutputPath -Force | Out-Null

# Publish GUI with self-contained deployment
Write-Host "Publishing GUI application..." -ForegroundColor Yellow
$GuiPublishPath = Join-Path $PlatformOutputPath "temp-gui"
dotnet publish src/DocxTemplate.UI/DocxTemplate.UI.csproj `
    --configuration $Configuration `
    --runtime $RuntimeId `
    --self-contained true `
    --output $GuiPublishPath `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    throw "GUI publish failed"
}

# Publish CLI with self-contained deployment
Write-Host "Publishing CLI application..." -ForegroundColor Yellow
$CliPublishPath = Join-Path $PlatformOutputPath "temp-cli"
dotnet publish src/DocxTemplate.CLI/DocxTemplate.CLI.csproj `
    --configuration $Configuration `
    --runtime $RuntimeId `
    --self-contained true `
    --output $CliPublishPath `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    throw "CLI publish failed"
}

# Copy executables to final distribution directory
Write-Host "Creating unified distribution..." -ForegroundColor Yellow

# Copy GUI executable
$GuiSource = Join-Path $GuiPublishPath $GuiExecutableName
$GuiTarget = Join-Path $PlatformOutputPath $GuiExecutableName
if (Test-Path $GuiSource) {
    Copy-Item $GuiSource $GuiTarget
} else {
    throw "GUI executable not found at $GuiSource"
}

# Copy CLI executable
$CliSource = Join-Path $CliPublishPath $CliExecutableName
$CliTarget = Join-Path $PlatformOutputPath $CliExecutableName
if (Test-Path $CliSource) {
    Copy-Item $CliSource $CliTarget
} else {
    throw "CLI executable not found at $CliSource"
}

# Clean up temporary directories
Remove-Item $GuiPublishPath -Recurse -Force
Remove-Item $CliPublishPath -Recurse -Force

# Create README for distribution
$ReadmeContent = @"
# DocxTemplate v$Version

This package contains both GUI and CLI versions of DocxTemplate.

## Contents

- ``$GuiExecutableName`` - Graphical user interface (wizard-based)
- ``$CliExecutableName`` - Command-line interface (for automation)

## Quick Start

### GUI Usage
Double-click ``$GuiExecutableName`` to start the wizard interface.

### CLI Usage
Open terminal/command prompt and run:
``````
./$CliExecutableName --help
``````

## Basic CLI Examples

List template sets:
``````
./$CliExecutableName list-sets --templates ./templates
``````

Discover templates:
``````
./$CliExecutableName discover --input ./templates --output json
``````

Scan for placeholders:
``````
./$CliExecutableName scan --input ./templates --output json
``````

Copy templates:
``````
./$CliExecutableName copy --source ./templates --destination ./output
``````

## System Requirements

- Self-contained applications (no .NET runtime installation required)
- $RuntimeId compatible system

## Support

For documentation and support, visit: https://github.com/your-org/docx-template-cli
"@

$ReadmePath = Join-Path $PlatformOutputPath "README.md"
$ReadmeContent | Out-File -FilePath $ReadmePath -Encoding utf8

# Verify executables work
Write-Host "Verifying executables..." -ForegroundColor Yellow

# Test CLI executable
$CliTestResult = & $CliTarget --help 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Warning "CLI executable verification failed: $CliTestResult"
} else {
    Write-Host "✓ CLI executable verified" -ForegroundColor Green
}

# Create package archive
$PackageName = "DocxTemplate-v$Version-$RuntimeId"
if ($RuntimeId -eq "win-x64") {
    $ArchivePath = Join-Path $OutputPath "$PackageName.zip"
    Write-Host "Creating ZIP package: $ArchivePath" -ForegroundColor Yellow
    
    # Use PowerShell's Compress-Archive
    Compress-Archive -Path "$PlatformOutputPath/*" -DestinationPath $ArchivePath -Force
} else {
    $ArchivePath = Join-Path $OutputPath "$PackageName.tar.gz"
    Write-Host "Creating TAR.GZ package: $ArchivePath" -ForegroundColor Yellow
    
    # Use tar command
    $CurrentDir = Get-Location
    try {
        Set-Location $OutputPath
        tar -czf "$PackageName.tar.gz" -C $RuntimeId .
    } finally {
        Set-Location $CurrentDir
    }
}

Write-Host "Distribution build completed successfully!" -ForegroundColor Green
Write-Host "Platform directory: $PlatformOutputPath" -ForegroundColor Cyan
Write-Host "Package archive: $ArchivePath" -ForegroundColor Cyan

# Display file sizes
Write-Host "`nFile sizes:" -ForegroundColor Yellow
Get-ChildItem $PlatformOutputPath | ForEach-Object {
    $SizeMB = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  $($_.Name): $SizeMB MB" -ForegroundColor White
}

if (Test-Path $ArchivePath) {
    $ArchiveSizeMB = [math]::Round((Get-Item $ArchivePath).Length / 1MB, 2)
    Write-Host "  Package: $ArchiveSizeMB MB" -ForegroundColor White
}