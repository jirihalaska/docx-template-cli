# DocxTemplate.Infrastructure

This project provides the infrastructure layer for the DocxTemplate application, implementing file system operations, configuration management, and dependency injection.

## Overview

The infrastructure layer contains:

- **File System Services**: Abstracted file operations with proper error handling
- **Configuration Management**: Multi-source configuration with validation
- **Dependency Injection**: Service registration and container setup

## Configuration

The infrastructure uses a hierarchical configuration system supporting:

1. **appsettings.json** - Base configuration
2. **Environment Variables** - Override base settings
3. **Command Line Arguments** - Override all other sources

### Configuration Structure

```json
{
  "Application": {
    "FileSystem": {
      "DefaultEncoding": "UTF-8",
      "MaxFileSizeMB": 50,
      "CreateBackupsOnReplace": true,
      "BackupSuffix": ".backup",
      "BackupRetentionDays": 30
    },
    "Templates": {
      "PlaceholderPattern": "\\{\\{.*?\\}\\}",
      "SupportedExtensions": [".docx"],
      "RecursiveSearch": true,
      "ExcludeDirectories": ["bin", "obj", ".git"],
      "MaxConcurrentOperations": 4
    },
    "Logging": {
      "LogLevel": "Information",
      "EnableFileLogging": false,
      "LogFilePath": "logs/docx-template-{Date}.log",
      "EnableConsoleLogging": true
    },
    "Performance": {
      "IoTimeoutSeconds": 30,
      "MemoryLimitMB": 512,
      "EnableParallelProcessing": true,
      "BatchSize": 10
    }
  }
}
```

## Services

### IFileSystemService

Provides abstracted file system operations:

```csharp
public interface IFileSystemService
{
    bool FileExists(string filePath);
    bool DirectoryExists(string directoryPath);
    
    Task<string> ReadAllTextAsync(string filePath, Encoding? encoding = null);
    Task WriteAllTextAsync(string filePath, string contents, Encoding? encoding = null);
    
    IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*");
    
    Task<string> CreateBackupAsync(string filePath);
    Task RestoreFromBackupAsync(string backupPath, string originalPath);
}
```

### IConfigurationService

Manages application configuration:

```csharp
public interface IConfigurationService
{
    ApplicationSettings GetSettings();
    T GetSection<T>(string sectionName) where T : class, new();
    void ValidateConfiguration();
    void ReloadConfiguration();
}
```

## Usage

### Setting up Infrastructure in Startup

```csharp
using DocxTemplate.Infrastructure.DependencyInjection;

// Basic setup
services.AddInfrastructure(configuration);

// Setup with validation
services.AddInfrastructureWithValidation(configuration);
```

### Using File System Service

```csharp
public class MyService
{
    private readonly IFileSystemService _fileSystem;
    
    public MyService(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    public async Task ProcessFileAsync(string filePath)
    {
        if (!_fileSystem.FileExists(filePath))
            throw new FileNotFoundException();
            
        var backup = await _fileSystem.CreateBackupAsync(filePath);
        
        try
        {
            var content = await _fileSystem.ReadAllTextAsync(filePath);
            var processed = ProcessContent(content);
            await _fileSystem.WriteAllTextAsync(filePath, processed);
        }
        catch (Exception)
        {
            await _fileSystem.RestoreFromBackupAsync(backup, filePath);
            throw;
        }
        finally
        {
            _fileSystem.DeleteBackup(backup);
        }
    }
}
```

### Using Configuration Service

```csharp
public class TemplateProcessor
{
    private readonly IConfigurationService _config;
    
    public TemplateProcessor(IConfigurationService config)
    {
        _config = config;
    }
    
    public void Initialize()
    {
        var settings = _config.GetSettings();
        var maxSize = settings.FileSystem.MaxFileSizeMB;
        var pattern = settings.Templates.PlaceholderPattern;
        
        // Use settings...
    }
}
```

## Error Handling

The infrastructure provides consistent error handling through:

- **FileAccessException**: For file system operation errors
- **Configuration Validation**: Automatic validation with detailed error messages
- **Argument Validation**: Comprehensive input validation

## Testing

The infrastructure includes comprehensive unit tests covering:

- File system operations (51 test cases)
- Configuration management
- Dependency injection setup
- Error scenarios and edge cases

Run tests with:
```bash
dotnet test tests/DocxTemplate.Infrastructure.Tests/
```

## Cross-Platform Support

All infrastructure components work consistently across:
- Windows
- macOS  
- Linux

File path handling and encoding are platform-aware.

## Performance

The infrastructure is optimized for:
- Concurrent file operations
- Memory efficiency
- Minimal overhead for configuration access
- Proper resource disposal