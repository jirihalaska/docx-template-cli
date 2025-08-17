using System.Text.Json;
using DocxTemplate.TestUtilities;

namespace DocxTemplate.EndToEnd.Tests.Utilities;

/// <summary>
/// Provisions and manages test environments for end-to-end testing
/// </summary>
public class TestEnvironmentProvisioner : IDisposable
{
    private readonly List<string> _temporaryDirectories = new();
    private readonly List<string> _temporaryFiles = new();
    private bool _disposed;

    /// <summary>
    /// Creates a complete test environment for workflow testing
    /// </summary>
    public async Task<TestEnvironment> CreateTestEnvironmentAsync(
        TestEnvironmentSpec spec,
        CancellationToken cancellationToken = default)
    {
        var environment = new TestEnvironment
        {
            Name = spec.Name,
            RootDirectory = CreateTemporaryDirectory($"e2e-test-{spec.Name}-{Guid.NewGuid():N}"),
            Spec = spec
        };

        try
        {
            // Create directory structure
            environment.TemplatesDirectory = Path.Combine(environment.RootDirectory, "templates");
            environment.OutputDirectory = Path.Combine(environment.RootDirectory, "output");
            environment.DataDirectory = Path.Combine(environment.RootDirectory, "data");
            
            Directory.CreateDirectory(environment.TemplatesDirectory);
            Directory.CreateDirectory(environment.OutputDirectory);
            Directory.CreateDirectory(environment.DataDirectory);

            // Copy template sets
            await CopyTemplateSetsAsync(spec.TemplateSets, environment.TemplatesDirectory);
            
            // Create test documents if specified
            if (spec.GenerateTestDocuments)
            {
                await GenerateTestDocumentsAsync(environment.TemplatesDirectory, spec.DocumentComplexity);
            }
            
            // Create replacement mapping files
            if (spec.ReplacementMappings.Any())
            {
                await CreateReplacementMappingFilesAsync(spec.ReplacementMappings, environment.DataDirectory);
            }
            
            // Create any additional test data
            if (spec.AdditionalTestFiles.Any())
            {
                await CreateAdditionalTestFilesAsync(spec.AdditionalTestFiles, environment.DataDirectory);
            }

            return environment;
        }
        catch
        {
            // Clean up on failure
            CleanupDirectory(environment.RootDirectory);
            throw;
        }
    }

    /// <summary>
    /// Creates a temporary directory for testing
    /// </summary>
    public string CreateTemporaryDirectory(string? name = null)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), name ?? $"docx-template-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        _temporaryDirectories.Add(tempDir);
        return tempDir;
    }

    /// <summary>
    /// Creates a temporary file for testing
    /// </summary>
    public string CreateTemporaryFile(string content, string extension = ".txt", string? name = null)
    {
        var fileName = name ?? $"test-{Guid.NewGuid():N}{extension}";
        var tempFile = Path.Combine(Path.GetTempPath(), fileName);
        File.WriteAllText(tempFile, content);
        _temporaryFiles.Add(tempFile);
        return tempFile;
    }

    private async Task CopyTemplateSetsAsync(List<TemplateSetSpec> templateSets, string targetDirectory)
    {
        foreach (var templateSet in templateSets)
        {
            var setDirectory = Path.Combine(targetDirectory, templateSet.Name);
            Directory.CreateDirectory(setDirectory);

            if (!string.IsNullOrEmpty(templateSet.SourcePath) && Directory.Exists(templateSet.SourcePath))
            {
                // Copy from existing template set
                await CopyDirectoryAsync(templateSet.SourcePath, setDirectory);
            }
            else
            {
                // Generate template set from specification
                await GenerateTemplateSetAsync(templateSet, setDirectory);
            }
        }
    }

    private async Task GenerateTemplateSetAsync(TemplateSetSpec spec, string targetDirectory)
    {
        var testDataManager = new TestDataManager();
        
        for (int i = 0; i < spec.DocumentCount; i++)
        {
            var fileName = $"{spec.Name}_Document_{i + 1:D2}.docx";
            var filePath = Path.Combine(targetDirectory, fileName);
            
            // Create a Word document with specified placeholders
            await testDataManager.CreateTestDocumentAsync(filePath, spec.Placeholders, spec.IncludeCzechCharacters);
        }
    }

    private async Task GenerateTestDocumentsAsync(string templatesDirectory, DocumentComplexity complexity)
    {
        var testDataManager = new TestDataManager();
        var complexitySpecs = GetComplexitySpecs(complexity);
        
        foreach (var complexitySpec in complexitySpecs)
        {
            var fileName = $"TestDoc_{complexitySpec.Name}.docx";
            var filePath = Path.Combine(templatesDirectory, "TestDocuments", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            
            await testDataManager.CreateComplexTestDocumentAsync(filePath, complexitySpec);
        }
    }

    private async Task CreateReplacementMappingFilesAsync(
        List<ReplacementMappingSpec> mappings, 
        string dataDirectory)
    {
        foreach (var mapping in mappings)
        {
            var fileName = $"{mapping.Name}.json";
            var filePath = Path.Combine(dataDirectory, fileName);
            
            var jsonContent = JsonSerializer.Serialize(mapping.Values, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(filePath, jsonContent);
        }
    }

    private async Task CreateAdditionalTestFilesAsync(
        List<AdditionalTestFileSpec> files, 
        string dataDirectory)
    {
        foreach (var file in files)
        {
            var filePath = Path.Combine(dataDirectory, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            
            await File.WriteAllTextAsync(filePath, file.Content);
        }
    }

    private async Task CopyDirectoryAsync(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(destDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile));
            File.Copy(file, destFile, true);
        }
    }

    private List<DocumentComplexitySpec> GetComplexitySpecs(DocumentComplexity complexity)
    {
        return complexity switch
        {
            DocumentComplexity.Simple => new List<DocumentComplexitySpec>
            {
                new() { Name = "Simple", HasTables = false, HasImages = false, PlaceholderCount = 5 }
            },
            DocumentComplexity.Medium => new List<DocumentComplexitySpec>
            {
                new() { Name = "SimpleTable", HasTables = true, HasImages = false, PlaceholderCount = 10 },
                new() { Name = "WithFormatting", HasTables = false, HasImages = false, PlaceholderCount = 8, HasComplexFormatting = true }
            },
            DocumentComplexity.Complex => new List<DocumentComplexitySpec>
            {
                new() { Name = "Full", HasTables = true, HasImages = true, PlaceholderCount = 20, HasComplexFormatting = true },
                new() { Name = "CzechHeavy", HasTables = true, HasImages = false, PlaceholderCount = 15, HasCzechCharacters = true }
            },
            _ => new List<DocumentComplexitySpec>()
        };
    }

    private void CleanupDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to cleanup directory {directory}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Clean up temporary directories
            foreach (var dir in _temporaryDirectories)
            {
                CleanupDirectory(dir);
            }
            
            // Clean up temporary files
            foreach (var file in _temporaryFiles)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to cleanup file {file}: {ex.Message}");
                }
            }
            
            _disposed = true;
        }
    }
}

/// <summary>
/// Specification for creating a test environment
/// </summary>
public class TestEnvironmentSpec
{
    public string Name { get; set; } = string.Empty;
    public List<TemplateSetSpec> TemplateSets { get; set; } = new();
    public bool GenerateTestDocuments { get; set; }
    public DocumentComplexity DocumentComplexity { get; set; } = DocumentComplexity.Simple;
    public List<ReplacementMappingSpec> ReplacementMappings { get; set; } = new();
    public List<AdditionalTestFileSpec> AdditionalTestFiles { get; set; } = new();
}

/// <summary>
/// Specification for a template set
/// </summary>
public class TemplateSetSpec
{
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public int DocumentCount { get; set; } = 3;
    public List<string> Placeholders { get; set; } = new();
    public bool IncludeCzechCharacters { get; set; }
}

/// <summary>
/// Specification for replacement mappings
/// </summary>
public class ReplacementMappingSpec
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Values { get; set; } = new();
}

/// <summary>
/// Specification for additional test files
/// </summary>
public class AdditionalTestFileSpec
{
    public string RelativePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Document complexity levels for testing
/// </summary>
public enum DocumentComplexity
{
    Simple,
    Medium,
    Complex
}

/// <summary>
/// Specification for document complexity
/// </summary>
public class DocumentComplexitySpec
{
    public string Name { get; set; } = string.Empty;
    public bool HasTables { get; set; }
    public bool HasImages { get; set; }
    public int PlaceholderCount { get; set; }
    public bool HasComplexFormatting { get; set; }
    public bool HasCzechCharacters { get; set; }
}

/// <summary>
/// Represents a complete test environment
/// </summary>
public class TestEnvironment
{
    public string Name { get; set; } = string.Empty;
    public string RootDirectory { get; set; } = string.Empty;
    public string TemplatesDirectory { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public string DataDirectory { get; set; } = string.Empty;
    public TestEnvironmentSpec Spec { get; set; } = new();
}