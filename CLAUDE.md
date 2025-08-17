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
```bash
# Create self-contained executables
dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r osx-x64 --self-contained
dotnet publish -c Release -r linux-x64 --self-contained
```

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
Currently in initial development phase. Core interfaces are defined but implementations are pending. The CLI entry point exists but commands are not yet implemented.

## Testing Conventions
- Unit tests use XUnit framework
- Use lowercase arrange, act, assert comments in tests
- Execute specific tests using `--filter` parameter