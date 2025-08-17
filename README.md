# DocxTemplate CLI

A modern, efficient command-line tool for processing Microsoft Word (.docx) document templates with placeholder replacement.

## Features

- **Template Discovery**: Automatically discover .docx template files in directories
- **Placeholder Scanning**: Scan documents for placeholders using configurable patterns (default: `{{placeholder}}`)
- **Smart Replacement**: Replace placeholders with values from JSON mapping files
- **Template Sets**: Manage collections of related templates as a unified set
- **Cross-Platform**: Built on .NET 9 for Windows, macOS, and Linux support
- **High Performance**: Optimized for processing large numbers of documents

## Architecture Principles

- **CLI-First**: All operations available as testable CLI commands
- **Separation of Concerns**: Core logic independent of UI/CLI
- **Modular Design**: Each operation is independent and composable  
- **Testability**: Every component individually testable
- **Pipeline Support**: Commands can be piped together

## Architecture

This application follows clean architecture principles with three distinct layers:

### 🎯 **DocxTemplate.CLI** 
Command-line interface layer handling user interaction and command parsing.

### 🔧 **DocxTemplate.Core**
Business logic layer containing service interfaces and domain models:
- `ITemplateDiscoveryService` - Template file discovery
- `IPlaceholderScanService` - Placeholder pattern scanning  
- `ITemplateCopyService` - Template copying operations
- `IPlaceholderReplaceService` - Placeholder replacement logic
- `ITemplateSetService` - Template set management

### 📁 **DocxTemplate.Infrastructure**
Implementation layer for file I/O and Word document processing using DocumentFormat.OpenXml.

## Commands

The CLI provides five main commands that work together to process DOCX templates:

### 1. List Template Sets
```bash
docx-template list-sets --templates /shared/templates
```
Discovers and lists all template sets (top-level directories containing .docx files).

### 2. Discover Templates  
```bash
docx-template discover --templates /shared/templates --set Contract_Templates
```
Finds all .docx files within a specific template set.

### 3. Scan Placeholders
```bash
docx-template scan --templates /shared/templates --set Contract_Templates [--pattern "{{.*?}}"]
```
Discovers all unique placeholders across templates in the set.

### 4. Copy Template Set
```bash
docx-template copy --templates /shared/templates --set Contract_Templates --target ./working
```
Copies an entire template set to a working directory with timestamp.

### 5. Replace Placeholders
```bash
docx-template replace --folder ./working/Contract_Templates_timestamp --map values.json
```
Replaces placeholders in all templates with values from mapping file.

## Complete Workflow Example
```bash
# Step 1: List available template sets
docx-template list-sets --templates /shared/templates

# Step 2: Explore a specific template set  
docx-template discover --templates /shared/templates --set Contract_Templates

# Step 3: Find placeholders in the set
docx-template scan --templates /shared/templates --set Contract_Templates

# Step 4: Copy template set to working directory
docx-template copy --templates /shared/templates --set Contract_Templates --target ./work

# Step 5: Replace placeholders with actual values
docx-template replace --folder ./work/Contract_Templates_timestamp --map contract-values.json
```

## Installation

```bash
# Build from source
dotnet build

# Run tests
dotnet test

# Create global tool
dotnet pack
dotnet tool install --global --add-source ./nupkg DocxTemplate.CLI
```

## Usage Examples

### Interactive Workflow
```bash
# Step 1: List available template sets
docx-template list-sets --templates /shared/templates

# Step 2: Discover templates in a specific set
docx-template discover --templates /shared/templates --set Contract_Templates

# Step 3: Find placeholders in the template set
docx-template scan --templates /shared/templates --set Contract_Templates

# Step 4: Copy template set to working directory
docx-template copy --templates /shared/templates --set Contract_Templates --target ./work

# Step 5: Replace placeholders with values
docx-template replace --folder ./work/Contract_Templates_timestamp --map values.json
```

### Pipeline Workflow
```bash
# JSON pipeline example
docx-template list-sets --templates /shared/templates --format json | \
  jq -r '.data.template_sets[0].name' | \
  xargs -I {} docx-template copy --templates /shared/templates --set {} --target ./work
```

### Replacement Map Format
```json
{
  "placeholders": {
    "{{COMPANY_NAME}}": "Acme Corp",
    "{{PROJECT_NAME}}": "Template System",
    "{{DATE}}": "2025-08-17",
    "{{AUTHOR}}": "John Doe"
  }
}
```

## Code Quality

This project uses:
- **StyleCop** for code style enforcement
- **Microsoft.CodeAnalysis.Analyzers** for code quality
- **EditorConfig** for consistent formatting
- **Automated testing** with xUnit, Moq, and FluentAssertions
- **Performance testing** with BenchmarkDotNet
- **GitHub Actions CI/CD** with automated build, test, and packaging

## Development

### Prerequisites

- .NET 9.0 SDK
- Git

### Project Structure
```
src/
├── DocxTemplate.CLI/          # CLI commands and entry point
├── DocxTemplate.Core/         # Business logic and interfaces
└── DocxTemplate.Infrastructure/ # File I/O and Word processing

tests/
├── DocxTemplate.CLI.Tests/    # CLI command tests
├── DocxTemplate.Core.Tests/   # Business logic tests
└── DocxTemplate.Integration.Tests/ # End-to-end tests
```

### Building
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Creating a Release
```bash
# Create self-contained executable for Windows
dotnet publish -c Release -r win-x64 --self-contained

# Create self-contained executable for macOS
dotnet publish -c Release -r osx-x64 --self-contained

# Create self-contained executable for Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

## Future UI Integration

This CLI is designed to be the backend for a future GUI application:

```csharp
// Future GUI can call CLI commands
var result = await CliWrapper.ExecuteAsync("list-sets", "--templates", templatesPath);

// Or use Core services directly
var templateSets = await templateSetService.DiscoverAsync(templatesPath);
var templates = await templateService.DiscoverAsync(templatesPath, setName);
```

## License

MIT