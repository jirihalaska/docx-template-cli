# DOCX Template CLI

A clean, modular command-line interface for Word document template processing with placeholder replacement.

## Architecture Principles

- **CLI-First**: All operations available as testable CLI commands
- **Separation of Concerns**: Core logic independent of UI/CLI
- **Modular Design**: Each operation is independent and composable  
- **Testability**: Every component individually testable
- **Pipeline Support**: Commands can be piped together

## Commands

### 1. Discover Templates
```bash
docx-template discover --folder ./templates [--recursive]
```
Finds all .docx files in the specified folder.

### 2. Scan Placeholders
```bash
docx-template scan --folder ./templates [--recursive] [--pattern "{{*}}"]
```
Discovers all unique placeholders across templates.

### 3. Copy Templates
```bash
docx-template copy --source ./templates --target ./output [--preserve-structure]
```
Copies templates to target folder, optionally preserving directory structure.

### 4. Replace Placeholders
```bash
docx-template replace --folder ./output --map replacements.json [--backup]
```
Replaces placeholders in all documents within the folder.

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
# Step 1: Discover what templates exist
docx-template discover --folder ./templates

# Step 2: Find what placeholders need values  
docx-template scan --folder ./templates --recursive

# Step 3: Copy templates to working directory
docx-template copy --source ./templates --target ./output

# Step 4: Replace placeholders with values
docx-template replace --folder ./output --map values.json
```

### Pipeline Workflow
```bash
# Complete pipeline
docx-template discover --folder ./templates | \
docx-template scan | \
docx-template copy --target ./output | \
docx-template replace --map values.json
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

## Development

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
var result = await CliWrapper.ExecuteAsync("discover", "--folder", folderPath);

// Or use Core services directly
var templates = await templateService.DiscoverAsync(folderPath);
```

## License

MIT