# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Build and Test
```bash
# Build the solution
dotnet build

# Run tests (when implemented)
dotnet test

# Run a specific test with XUnit filter
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

### CLI Development
```bash
# Run the CLI during development
dotnet run --project src/DocxTemplate.CLI

# Build and install as global tool
dotnet pack
dotnet tool install --global --add-source ./nupkg DocxTemplate.CLI
```

### Publishing

#### CLI Only (Traditional)
```bash
# Create self-contained CLI executables
dotnet publish src/DocxTemplate.CLI -c Release -r win-x64 --self-contained
dotnet publish src/DocxTemplate.CLI -c Release -r osx-x64 --self-contained
dotnet publish src/DocxTemplate.CLI -c Release -r linux-x64 --self-contained
```

#### GUI Application (Self-Contained)
```bash
# Create self-contained GUI applications with embedded CLI
dotnet publish src/DocxTemplate.UI -c Release -r win-x64 --self-contained -p:SkipCliBuild=true
dotnet publish src/DocxTemplate.UI -c Release -r osx-arm64 --self-contained -p:SkipCliBuild=true
dotnet publish src/DocxTemplate.UI -c Release -r linux-x64 --self-contained -p:SkipCliBuild=true
```

#### Complete Release Build (Recommended)
```bash
# Build both CLI and GUI together for all platforms
./scripts/build-release.sh

# Or build just GUI for current platform (with app bundle on macOS)
./scripts/publish-gui.sh

# Build GUI for specific platform
./scripts/publish-gui.sh win-x64
./scripts/publish-gui.sh osx-arm64
./scripts/publish-gui.sh linux-x64
```

#### Platform-Specific Builds
```bash
# Windows: Multiple distribution formats
./scripts/build-windows.sh

# macOS: App bundle with double-click support  
./scripts/publish-gui.sh osx-arm64

# Linux: Standard tarball
./scripts/publish-gui.sh linux-x64
```

#### Windows Distribution Options
1. **Portable ZIP** (Recommended): Extract and run, no installation
2. **Single-file EXE**: Minimal footprint, slower first startup  
3. **Developer package**: Includes debug symbols for troubleshooting

#### macOS Distribution
- **App Bundle**: Native `.app` format for double-click launching
- Self-contained with all dependencies in `Contents/MacOS/`

## Architecture

This is a clean, modular .NET 9 command-line application for Word document template processing with placeholder replacement, following these architectural principles:

### Three-Layer Architecture
1. **DocxTemplate.CLI** - Command-line interface layer, handles user interaction and command parsing
2. **DocxTemplate.Core** - Business logic layer with service interfaces and domain models
3. **DocxTemplate.Infrastructure** - Implementation layer for file I/O and Word document processing

### Core Service Interfaces
- `ITemplateDiscoveryService` - Discovers .docx files in specified directories
- `IPlaceholderScanService` - Scans documents for placeholder patterns (default: `{{.*?}}`)
- `ITemplateCopyService` - Copies templates to target directories
- `IPlaceholderReplaceService` - Replaces placeholders with values from JSON mapping
- `ITemplateSetService` - Manages sets of templates as a collection

### Domain Models
- `TemplateFile` - Represents a discovered template with metadata
- `PlaceholderScanResult` - Contains discovered placeholders and scan statistics
- `Placeholder` - Represents a unique placeholder with its locations
- `PlaceholderLocation` - Tracks where placeholders appear in templates

### Implementation Status
CLI commands are implemented and functional:
- `discover` - Find DOCX files in directories
- `scan` - Find placeholders in DOCX files using regex patterns  
- `copy` - Copy templates with performance metrics
- `list-sets` - List template sets (directories)

All commands support JSON output format for programmatic use.

## Testing Conventions
- Unit tests use XUnit framework
- Use lowercase arrange, act, assert comments in tests
- Execute specific tests using `--filter` parameter

### Executable Testing
```bash
# macOS/Linux: Test executable startup functionality
./scripts/test-executables.sh

# Windows: Test executable startup functionality  
./scripts/test-executables.ps1

# Test specific platform
./scripts/test-executables.sh osx-arm64
./scripts/test-executables.ps1 -Platform "win-x64"

# CI automatically tests platform-native executables:
# - Windows runner: Tests win-x64 using PowerShell
# - macOS runner: Tests osx-arm64 using bash
# See .github/workflows/ci.yml for automated testing
```

## CLI Documentation Maintenance
**IMPORTANT**: When making changes to CLI commands, you MUST update the documentation:
- File: `docs/cli-reference.md`
- Update command parameters, output schemas, and behavior
- Test actual CLI output and update examples
- The documentation must reflect the current CLI implementation exactly