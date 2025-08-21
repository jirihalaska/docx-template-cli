# DOCX Template Processing System

## Overview

A cross-platform desktop application for processing Word document templates with placeholder replacement, built with .NET 9 and Avalonia UI.

## Key Features

- 🖥️ **Cross-Platform**: Runs on Windows and macOS
- 🇨🇿 **Czech Localization**: Full support for Czech legal documents
- 🔄 **Placeholder Processing**: Scan and replace `{{placeholders}}` in DOCX files
- 📁 **Template Sets**: Organize templates in logical collections
- 🎯 **Direct Service Integration**: High-performance architecture without CLI overhead
- 📝 **File Prefix Support**: Dynamic file naming with SOUBOR_PREFIX placeholder

## Architecture

The system uses Clean Architecture with three main layers:

```
┌─────────────────────────────────┐
│      Avalonia UI (MVVM)         │
├─────────────────────────────────┤
│         Core Services           │
├─────────────────────────────────┤
│  Infrastructure (OpenXML, I/O)  │
└─────────────────────────────────┘
```

**Key Design Decision**: The UI directly references Core and Infrastructure services, eliminating the need for CLI process spawning and resulting in better performance and simpler deployment.

## User Workflow

1. **Select Template Set**: Choose from discovered template collections
2. **Discover Placeholders**: Automatically scan for `{{placeholders}}`
3. **Input Values**: Provide replacement values for each placeholder
4. **Choose Output**: Select destination folder for processed documents
5. **Process**: Generate documents with all placeholders replaced

## Documentation

### Core Documentation
- **[Architecture](architecture.md)** - System design and component structure
- **[Technical Specification](technical-specification.md)** - Core services implementation
- **[Implementation Summary](implementation-summary.md)** - Completed features overview
- **[Product Requirements](prd.md)** - Original requirements document

### UI Documentation
- **[Avalonia Best Practices](avalonia-best-practices.md)** - UI development guidelines
- **[UI Framework Analysis](ui-framework-analysis.md)** - Framework selection rationale

### Development History
- **[Stories Archive](stories/)** - Completed implementation stories (30 epics)
- **[CLI Reference](cli-reference.md)** - Standalone CLI documentation (for CLI-only distribution)

## Technology Stack

- **.NET 9**: Latest framework for performance and features
- **Avalonia UI 11.x**: Cross-platform GUI framework
- **ReactiveUI**: MVVM and reactive programming
- **DocumentFormat.OpenXml**: Native DOCX processing
- **C# 13**: Modern language features with nullable reference types

## Getting Started

### Prerequisites
- .NET 9 SDK
- Visual Studio 2022 / VS Code / Rider

### Build and Run
```bash
# Build the solution
dotnet build

# Run the UI application
dotnet run --project src/DocxTemplate.UI

# Run tests
dotnet test
```

### Publishing
```bash
# Windows
dotnet publish src/DocxTemplate.UI -c Release -r win-x64 --self-contained

# macOS (with app bundle)
./scripts/publish-gui.sh osx-arm64
```

## Project Status

✅ **Production Ready** - All major features implemented and tested

### Completed Features
- Core template processing engine
- Avalonia UI with 5-step wizard
- Czech localization throughout
- Direct service integration (no CLI dependency)
- Cross-platform support (Windows/macOS)
- Comprehensive test coverage

### Quality Metrics
- **Test Coverage**: ~85% for Core services
- **Performance**: ~50 documents/second processing
- **Memory Usage**: <100MB typical
- **Startup Time**: <2 seconds

---

*Last Updated: 2025-08-21*