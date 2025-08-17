# Technical Specification
**DOCX Template CLI System v2.0**  
*BMad-Compliant Technical Specification*  
*Created: 2025-08-17*

---

## Technical Overview

A command-line interface system for Word document template processing built on .NET 8.0 with Clean Architecture, designed for high performance, cross-platform compatibility, and future GUI integration.

---

## Technology Stack

### Core Technologies
- **Runtime**: .NET 8.0 (LTS)
- **Language**: C# 12 with nullable reference types
- **CLI Framework**: System.CommandLine (2.0.0-beta)
- **Word Processing**: DocumentFormat.OpenXml (3.0.0)
- **Testing**: xUnit 2.6, Moq 4.20, FluentAssertions 6.12
- **Benchmarking**: BenchmarkDotNet 0.13

### Development Tools
- **IDE**: Visual Studio 2022 / VS Code / Rider
- **Version Control**: Git with conventional commits
- **CI/CD**: GitHub Actions
- **Code Quality**: SonarQube, Roslyn Analyzers
- **Documentation**: DocFX for API docs

---

## Implementation Details

### 1. Template Discovery Implementation

#### File System Scanning
```csharp
public class TemplateDiscoveryService : ITemplateDiscoveryService
{
    private readonly IFileSystemService _fileSystem;
    
    public async Task<IEnumerable<TemplateFile>> DiscoverTemplatesAsync(
        string folderPath, 
        bool recursive,
        CancellationToken cancellationToken)
    {
        // Validate path
        if (!_fileSystem.DirectoryExists(folderPath))
            throw new DirectoryNotFoundException($"Path not found: {folderPath}");
            
        // Enumerate files
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = _fileSystem.EnumerateFiles(folderPath, "*.docx", searchOption);
        
        // Process in parallel with throttling
        var semaphore = new SemaphoreSlim(10); // Max 10 concurrent
        var tasks = files.Select(async file =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var info = await _fileSystem.GetFileInfoAsync(file);
                return new TemplateFile
                {
                    FullPath = file,
                    RelativePath = Path.GetRelativePath(folderPath, file),
                    FileName = Path.GetFileName(file),
                    SizeInBytes = info.Length,
                    LastModified = info.LastWriteTimeUtc
                };
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        return await Task.WhenAll(tasks);
    }
}
```

#### Performance Optimizations
- Async enumeration for large directories
- Parallel file info retrieval with throttling
- Lazy evaluation where possible
- Memory-mapped files for large documents

### 2. Placeholder Scanning Implementation

#### OpenXML Document Processing
```csharp
public class PlaceholderScanner : IPlaceholderScanner
{
    private readonly Regex _placeholderRegex;
    
    public PlaceholderScanner(string pattern = @"\{\{([^}]+)\}\}")
    {
        _placeholderRegex = new Regex(pattern, RegexOptions.Compiled);
    }
    
    public async Task<IEnumerable<Placeholder>> ScanDocumentAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var placeholders = new Dictionary<string, List<Location>>();
        
        await Task.Run(() =>
        {
            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart.Document.Body;
            
            // Scan all text elements
            foreach (var text in body.Descendants<Text>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var matches = _placeholderRegex.Matches(text.Text);
                foreach (Match match in matches)
                {
                    var placeholder = match.Value;
                    if (!placeholders.ContainsKey(placeholder))
                        placeholders[placeholder] = new List<Location>();
                        
                    placeholders[placeholder].Add(new Location
                    {
                        ElementId = text.Parent?.Parent?.GetAttribute("id", "").Value,
                        Position = match.Index
                    });
                }
            }
            
            // Also scan headers, footers, footnotes
            ScanHeadersFooters(doc, placeholders);
            ScanComments(doc, placeholders);
        }, cancellationToken);
        
        return placeholders.Select(p => new Placeholder
        {
            Name = p.Key,
            Pattern = p.Key,
            Locations = p.Value.Select(l => new PlaceholderLocation
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                Occurrences = p.Value.Count
            }),
            TotalOccurrences = p.Value.Count
        });
    }
}
```

#### Text Run Handling
```csharp
// Handle placeholders split across multiple runs
private string CombineRuns(IEnumerable<Run> runs)
{
    var sb = new StringBuilder();
    foreach (var run in runs)
    {
        var text = run.GetFirstChild<Text>();
        if (text != null)
            sb.Append(text.Text);
    }
    return sb.ToString();
}
```

### 3. Template Copying Implementation

#### Efficient File Copying
```csharp
public class TemplateCopyService : ITemplateCopyService
{
    private readonly IFileSystemService _fileSystem;
    
    public async Task<CopyResult> CopyTemplatesAsync(
        string sourcePath,
        string targetPath,
        bool preserveStructure,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var copiedFiles = new List<CopiedFile>();
        
        // Create target directory
        await _fileSystem.CreateDirectoryAsync(targetPath);
        
        // Get all templates
        var templates = await _fileSystem.EnumerateFilesAsync(
            sourcePath, "*.docx", SearchOption.AllDirectories);
        
        // Copy in parallel with progress reporting
        var progress = new Progress<CopyProgress>();
        await Parallel.ForEachAsync(templates, 
            new ParallelOptions 
            { 
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            },
            async (template, ct) =>
            {
                var targetFile = preserveStructure
                    ? Path.Combine(targetPath, Path.GetRelativePath(sourcePath, template))
                    : Path.Combine(targetPath, Path.GetFileName(template));
                
                // Check overwrite
                if (!overwrite && await _fileSystem.FileExistsAsync(targetFile))
                    return;
                
                // Ensure target directory exists
                var targetDir = Path.GetDirectoryName(targetFile);
                await _fileSystem.CreateDirectoryAsync(targetDir);
                
                // Copy with retry logic
                await CopyWithRetryAsync(template, targetFile, ct);
                
                var fileInfo = await _fileSystem.GetFileInfoAsync(targetFile);
                copiedFiles.Add(new CopiedFile
                {
                    SourcePath = template,
                    TargetPath = targetFile,
                    SizeInBytes = fileInfo.Length
                });
                
                progress.Report(new CopyProgress
                {
                    Current = copiedFiles.Count,
                    Total = templates.Count()
                });
            });
        
        return new CopyResult
        {
            FilesCount = copiedFiles.Count,
            TotalBytesCount = copiedFiles.Sum(f => f.SizeInBytes),
            CopiedFiles = copiedFiles,
            Duration = stopwatch.Elapsed
        };
    }
    
    private async Task CopyWithRetryAsync(
        string source, 
        string target, 
        CancellationToken cancellationToken,
        int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await _fileSystem.CopyFileAsync(source, target, cancellationToken);
                return;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100 * (i + 1)), cancellationToken);
            }
        }
    }
}
```

### 4. Placeholder Replacement Implementation

#### Atomic Replacement with Backup
```csharp
public class PlaceholderReplaceService : IPlaceholderReplaceService
{
    private readonly IDocumentProcessor _processor;
    private readonly IBackupService _backup;
    
    public async Task<ReplaceResult> ReplacePlaceholdersAsync(
        string folderPath,
        Dictionary<string, string> replacements,
        bool createBackup,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var fileResults = new ConcurrentBag<FileReplaceResult>();
        
        // Get all documents
        var documents = await _fileSystem.EnumerateFilesAsync(
            folderPath, "*.docx", SearchOption.AllDirectories);
        
        // Process each document
        await Parallel.ForEachAsync(documents,
            new ParallelOptions { CancellationToken = cancellationToken },
            async (docPath, ct) =>
            {
                var result = await ProcessDocumentAsync(
                    docPath, replacements, createBackup, ct);
                fileResults.Add(result);
            });
        
        return new ReplaceResult
        {
            FilesProcessed = fileResults.Count,
            TotalReplacements = fileResults.Sum(r => r.ReplacementCount),
            FileResults = fileResults,
            Duration = stopwatch.Elapsed,
            HasErrors = fileResults.Any(r => !r.Success)
        };
    }
    
    private async Task<FileReplaceResult> ProcessDocumentAsync(
        string filePath,
        Dictionary<string, string> replacements,
        bool createBackup,
        CancellationToken cancellationToken)
    {
        string backupPath = null;
        
        try
        {
            // Create backup if requested
            if (createBackup)
            {
                backupPath = await _backup.CreateBackupAsync(filePath);
            }
            
            // Process document
            int replacementCount = 0;
            
            await Task.Run(() =>
            {
                using var doc = WordprocessingDocument.Open(filePath, true);
                
                // Replace in body
                replacementCount += ReplaceInElement(
                    doc.MainDocumentPart.Document.Body, replacements);
                
                // Replace in headers/footers
                foreach (var header in doc.MainDocumentPart.HeaderParts)
                {
                    replacementCount += ReplaceInElement(header.Header, replacements);
                }
                
                foreach (var footer in doc.MainDocumentPart.FooterParts)
                {
                    replacementCount += ReplaceInElement(footer.Footer, replacements);
                }
                
                doc.Save();
            }, cancellationToken);
            
            return new FileReplaceResult
            {
                FilePath = filePath,
                ReplacementCount = replacementCount,
                Success = true,
                BackupPath = backupPath
            };
        }
        catch (Exception ex)
        {
            // Restore from backup on error
            if (backupPath != null)
            {
                await _backup.RestoreAsync(backupPath, filePath);
            }
            
            return new FileReplaceResult
            {
                FilePath = filePath,
                ReplacementCount = 0,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    private int ReplaceInElement(OpenXmlElement element, Dictionary<string, string> replacements)
    {
        int count = 0;
        
        // Handle text elements
        foreach (var text in element.Descendants<Text>())
        {
            foreach (var replacement in replacements)
            {
                if (text.Text.Contains(replacement.Key))
                {
                    text.Text = text.Text.Replace(replacement.Key, replacement.Value);
                    count++;
                }
            }
        }
        
        return count;
    }
}
```

---

## Error Handling Specifications

### Exception Hierarchy
```csharp
public abstract class DocxTemplateException : Exception
{
    public string ErrorCode { get; }
    public ErrorSeverity Severity { get; }
}

public class FileAccessException : DocxTemplateException
{
    public string FilePath { get; }
    public FileAccessError ErrorType { get; }
}

public class PlaceholderException : DocxTemplateException
{
    public string PlaceholderName { get; }
    public PlaceholderError ErrorType { get; }
}

public class ValidationException : DocxTemplateException
{
    public IEnumerable<ValidationError> Errors { get; }
}
```

### Retry Policies
```csharp
// Polly retry policy for I/O operations
var retryPolicy = Policy
    .Handle<IOException>()
    .WaitAndRetryAsync(
        3,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (exception, timeSpan, retryCount, context) =>
        {
            _logger.LogWarning($"Retry {retryCount} after {timeSpan}s");
        });
```

---

## Performance Specifications

### Benchmarks
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class TemplateBenchmarks
{
    [Params(10, 100, 1000)]
    public int FileCount { get; set; }
    
    [Benchmark]
    public async Task DiscoverTemplates()
    {
        var service = new TemplateDiscoveryService();
        await service.DiscoverTemplatesAsync("./test-data", true);
    }
    
    [Benchmark]
    public async Task ScanPlaceholders()
    {
        var scanner = new PlaceholderScanner();
        await scanner.ScanDocumentAsync("./test.docx");
    }
    
    [Benchmark]
    public async Task ReplaceInDocument()
    {
        var replacer = new PlaceholderReplaceService();
        await replacer.ReplacePlaceholdersAsync("./test.docx", GetReplacements());
    }
}
```

### Performance Targets
| Operation | Files | Target Time | Max Memory |
|-----------|-------|-------------|------------|
| Discover | 100 | <1s | 50MB |
| Scan | 100 | <5s | 100MB |
| Copy | 100 | <10s | 150MB |
| Replace | 100 | <10s | 200MB |

---

## Security Specifications

### Input Validation
```csharp
public class PathValidator
{
    public ValidationResult ValidatePath(string path)
    {
        // Prevent directory traversal
        if (path.Contains(".."))
            return ValidationResult.Error("Path traversal detected");
            
        // Validate absolute path
        if (!Path.IsPathRooted(path))
            return ValidationResult.Error("Path must be absolute");
            
        // Validate path length
        if (path.Length > 260) // Windows MAX_PATH
            return ValidationResult.Error("Path too long");
            
        return ValidationResult.Success();
    }
}
```

### Sanitization
```csharp
public class PlaceholderSanitizer
{
    public string Sanitize(string input)
    {
        // Remove control characters
        input = Regex.Replace(input, @"[\x00-\x1F\x7F]", "");
        
        // Limit length
        if (input.Length > 1000)
            input = input.Substring(0, 1000);
            
        return input;
    }
}
```

---

## Testing Specifications

### Unit Test Coverage Requirements
- Minimum 90% code coverage
- 100% coverage for critical paths
- All public APIs must have tests
- Edge cases and error conditions tested

### Integration Test Scenarios
1. **End-to-end workflow test**
2. **Large file handling (>100MB)**
3. **Concurrent operations**
4. **Network drive access**
5. **Permission denied handling**
6. **Corrupt document handling**

### Performance Test Suite
```csharp
[Collection("Performance")]
public class PerformanceTests
{
    [Fact]
    public async Task Process100Files_Under10Seconds()
    {
        // arrange
        var files = GenerateTestFiles(100);
        var processor = new TemplateProcessor();
        
        // act
        var stopwatch = Stopwatch.StartNew();
        await processor.ProcessAllAsync(files);
        stopwatch.Stop();
        
        // assert
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10));
    }
}
```

---

## Deployment Specifications

### Build Configuration
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <PublishReadyToRun>true</PublishReadyToRun>
    <PublishTrimmed>true</PublishTrimmed>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
  </PropertyGroup>
</Project>
```

### Platform-Specific Builds
```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# macOS Intel
dotnet publish -c Release -r osx-x64 --self-contained

# macOS Apple Silicon
dotnet publish -c Release -r osx-arm64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

---

*This technical specification provides detailed implementation guidance for the DOCX Template CLI v2.0 system, ensuring consistent, high-quality development.*