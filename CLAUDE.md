# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Build and Test
```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run specific test projects
dotnet test tests/DocxTemplate.Core.Tests
dotnet test tests/DocxTemplate.Infrastructure.Tests
dotnet test tests/DocxTemplate.UI.Tests
dotnet test tests/DocxTemplate.EndToEnd.Tests

# Run a specific test with XUnit filter
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

### GUI Development
```bash
# Run the GUI during development
dotnet run --project src/DocxTemplate.UI

# Build GUI for current platform
dotnet build src/DocxTemplate.UI -c Release
```

### Publishing

#### GUI Application (Self-Contained)
```bash
# Create self-contained GUI applications
dotnet publish src/DocxTemplate.UI -c Release -r win-x64 --self-contained -p:SkipCliBuild=true
dotnet publish src/DocxTemplate.UI -c Release -r osx-arm64 --self-contained -p:SkipCliBuild=true
dotnet publish src/DocxTemplate.UI -c Release -r linux-x64 --self-contained -p:SkipCliBuild=true
```

#### Complete Release Build (Recommended)
```bash
# Build GUI for current platform (with app bundle on macOS)
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

This is a clean, modular .NET 9 GUI application for Word document template processing with placeholder replacement, including image placeholders.

### Three-Layer Architecture
1. **DocxTemplate.UI** - Avalonia-based GUI application with MVVM pattern
2. **DocxTemplate.Core** - Business logic layer with service interfaces and domain models
3. **DocxTemplate.Infrastructure** - Implementation layer for file I/O and Word document processing

### Core Service Interfaces
- `ITemplateDiscoveryService` - Discovers .docx files in specified directories
- `IPlaceholderScanService` - Scans documents for placeholder patterns (default: `{{.*?}}`)
- `ITemplateCopyService` - Copies templates to target directories
- `IPlaceholderReplaceService` - Replaces text and image placeholders with values from JSON mapping
- `ITemplateSetService` - Manages sets of templates as a collection

### Domain Models
- `TemplateFile` - Represents a discovered template with metadata
- `PlaceholderScanResult` - Contains discovered placeholders and scan statistics
- `Placeholder` - Represents a unique placeholder with its locations
- `PlaceholderLocation` - Tracks where placeholders appear in templates
- `PlaceholderReplaceResult` - Result of placeholder replacement operation

### Key Features
- Text placeholder replacement: `{{PLACEHOLDER_NAME}}`
- Image placeholder replacement: Detects and replaces image placeholders
- Support for headers, footers, and document body
- Batch processing of multiple templates
- JSON-based placeholder mapping

## Testing Conventions
- Unit tests use XUnit framework
- Use lowercase arrange, act, assert comments in tests
- Execute specific tests using `--filter` parameter
- FluentAssertions for test assertions
- Moq for mocking dependencies

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
# - macOS runner: Tests osx-arm64 using bash (currently disabled)
# See .github/workflows/ci.yml for automated testing
```

## UI Framework
- **Avalonia UI 11.3.4**: Cross-platform .NET UI framework
- **ReactiveUI**: MVVM framework for reactive programming
- **Fluent Theme**: Modern, consistent UI design
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection for IoC

## Placeholder Replacement
### Text Placeholders
- Pattern: `{{PLACEHOLDER_NAME}}`
- Case-insensitive matching
- Supports nested placeholders in tables and lists
- Preserves formatting of surrounding text

### Image Placeholders  
- Automatically detects image placeholders in documents
- Replaces with specified image files from mapping
- Maintains image dimensions and positioning
- Supports images in headers, footers, and body

### Mapping File Format
```json
{
  "placeholders": {
    "{{COMPANY_NAME}}": "Acme Corp",
    "{{PROJECT_NAME}}": "Template System",
    "{{DATE}}": "2025-08-17",
    "{{LOGO}}": "path/to/logo.png",
    "{{SIGNATURE}}": "path/to/signature.jpg"
  }
}
```

## Technical Documentation
For detailed information about the DOCX processing system, see [docs/docx-processing.md](docs/docx-processing.md), which covers:
- Text run splitting problem and solutions
- Placeholder detection algorithms
- Replacement strategies and error handling
- Processing architecture and performance considerations

## Important Notes
- The standalone CLI project has been removed (commit 278bfe4)
- CLI functionality is now provided through pre-built executables distributed with the GUI
- The UI project includes templates folder that gets copied during build
- All document processing is done through the Infrastructure layer using DocumentFormat.OpenXml