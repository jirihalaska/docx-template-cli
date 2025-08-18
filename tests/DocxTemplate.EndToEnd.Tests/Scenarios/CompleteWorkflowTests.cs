using DocxTemplate.EndToEnd.Tests.Utilities;
using FluentAssertions;
using System.Text.Json;

namespace DocxTemplate.EndToEnd.Tests.Scenarios;

/// <summary>
/// End-to-end tests for complete user workflows
/// </summary>
public class CompleteWorkflowTests : IDisposable
{
    private readonly CliProcessExecutor _cliExecutor;
    private readonly TestEnvironmentProvisioner _environmentProvisioner;
    private readonly DocumentIntegrityValidator _documentValidator;
    private readonly List<TestEnvironment> _testEnvironments = new();

    public CompleteWorkflowTests()
    {
        var cliPath = CliProcessExecutor.GetCliExecutablePath();
        _cliExecutor = new CliProcessExecutor(cliPath);
        _environmentProvisioner = new TestEnvironmentProvisioner();
        _documentValidator = new DocumentIntegrityValidator();
    }

    /// <summary>
    /// Tests complete workflow: list-sets → discover → scan → copy → replace
    /// </summary>
    [Fact]
    public async Task CompleteWorkflow_ProcessesTemplateSetSuccessfully()
    {
        // arrange
        var environment = await CreateTestEnvironmentAsync("CompleteWorkflow");

        // act & assert - Execute complete workflow
        
        // Step 1: List template sets
        var listResult = await _cliExecutor.ExecuteAsync(
            $"list-sets --templates \"{environment.TemplatesDirectory}\"", 
            environment.RootDirectory);
        
        listResult.IsSuccess.Should().BeTrue($"list-sets should succeed. Error: {listResult.StandardError}");
        listResult.StandardOutput.Should().Contain("TestTemplateSet");

        // Step 2: Discover templates in the TestTemplateSet directory
        var templateSetPath = Path.Combine(environment.TemplatesDirectory, "TestTemplateSet");
        var discoverResult = await _cliExecutor.ExecuteAsync(
            $"discover --path \"{templateSetPath}\"", 
            environment.RootDirectory);
        
        discoverResult.IsSuccess.Should().BeTrue($"discover should succeed. Error: {discoverResult.StandardError}");

        // Step 3: Scan for placeholders
        var scanResult = await _cliExecutor.ExecuteAsync(
            $"scan --path \"{templateSetPath}\"", 
            environment.RootDirectory);
        
        scanResult.IsSuccess.Should().BeTrue($"scan should succeed. Error: {scanResult.StandardError}");

        // Step 4: Copy templates to output directory
        var copyResult = await _cliExecutor.ExecuteAsync(
            $"copy --source \"{templateSetPath}\" --target \"{environment.OutputDirectory}\"", 
            environment.RootDirectory);
        
        copyResult.IsSuccess.Should().BeTrue($"copy should succeed. Error: {copyResult.StandardError}");
        
        // Verify files were copied
        var outputFiles = Directory.GetFiles(environment.OutputDirectory, "*.docx", SearchOption.AllDirectories);
        outputFiles.Should().NotBeEmpty("copied files should exist in output directory");

        // Step 5: Replace placeholders with values
        var replacementMapping = CreateReplacementMapping();
        var mappingFile = Path.Combine(environment.DataDirectory, "replacements.json");
        await File.WriteAllTextAsync(mappingFile, JsonSerializer.Serialize(new { placeholders = replacementMapping }, new JsonSerializerOptions { WriteIndented = true }));
        
        var replaceResult = await _cliExecutor.ExecuteAsync(
            $"replace --folder \"{environment.OutputDirectory}\" --map \"{mappingFile}\"", 
            environment.RootDirectory);
        
        replaceResult.IsSuccess.Should().BeTrue($"replace should succeed. Error: {replaceResult.StandardError}");

        // Verify document integrity after complete workflow
        await ValidateProcessedDocumentsAsync(environment);
    }

    /// <summary>
    /// Tests workflow with Czech character preservation
    /// </summary>
    [Fact]
    public async Task CompleteWorkflow_PreservesCzechCharacters()
    {
        // arrange
        var environment = await CreateTestEnvironmentWithCzechCharactersAsync();
        var czechReplacementMapping = CreateCzechReplacementMapping();
        var mappingFile = Path.Combine(environment.DataDirectory, "czech_replacements.json");
        await File.WriteAllTextAsync(mappingFile, JsonSerializer.Serialize(czechReplacementMapping, new JsonSerializerOptions { WriteIndented = true }));

        // act - Execute workflow with Czech documents (using available commands)
        var czechTemplateSetPath = Path.Combine(environment.TemplatesDirectory, "CzechTestSet");
        
        var copyResult = await _cliExecutor.ExecuteAsync(
            $"copy --source \"{czechTemplateSetPath}\" --target \"{environment.OutputDirectory}\"", 
            environment.RootDirectory);

        // assert - For now just verify copy works, replace not implemented yet
        copyResult.IsSuccess.Should().BeTrue($"copy with Czech characters should succeed. Error: {copyResult.StandardError}");
        
        // Step 5: Replace placeholders with Czech values
        var replaceResult = await _cliExecutor.ExecuteAsync(
            $"replace --folder \"{environment.OutputDirectory}\" --map \"{mappingFile}\"", 
            environment.RootDirectory);
        
        replaceResult.IsSuccess.Should().BeTrue($"replace with Czech characters should succeed. Error: {replaceResult.StandardError}");

        // Validate Czech character preservation
        var outputFiles = Directory.GetFiles(environment.OutputDirectory, "*.docx", SearchOption.AllDirectories);
        foreach (var outputFile in outputFiles)
        {
            var originalFile = FindCorrespondingOriginalFile(outputFile, environment);
            if (originalFile != null)
            {
                var validation = await _documentValidator.ValidateCharacterPreservationAsync(originalFile, outputFile);
                validation.IsCharactersPreserved.Should().BeTrue($"Czech characters should be preserved in {outputFile}. Issues: {string.Join(", ", validation.Issues)}");
            }
        }
    }

    /// <summary>
    /// Tests command chaining with JSON output as input
    /// </summary>
    [Fact]
    public async Task CompleteWorkflow_CommandChaining_JsonPipeline()
    {
        // arrange
        var environment = await CreateTestEnvironmentAsync("CommandChaining");

        // act - Test JSON output from one command as context for another
        
        // Get template set list as JSON
        var listResult = await _cliExecutor.ExecuteAsync(
            $"list-sets --templates \"{environment.TemplatesDirectory}\" --format json", 
            environment.RootDirectory);
        
        listResult.IsSuccess.Should().BeTrue();
        
        // Parse the JSON output and use it in subsequent commands
        var jsonContent = CliProcessExecutor.ExtractJsonFromOutput(listResult.StandardOutput);
        var setsData = JsonSerializer.Deserialize<JsonElement>(jsonContent);
        setsData.TryGetProperty("data", out var data).Should().BeTrue("JSON should contain data");
        data.TryGetProperty("template_sets", out var templateSets).Should().BeTrue("JSON data should contain template_sets");
        
        // Use discovered template set information in subsequent command
        var templateSetPath = Path.Combine(environment.TemplatesDirectory, "TestTemplateSet");
        var discoverResult = await _cliExecutor.ExecuteAsync(
            $"discover --path \"{templateSetPath}\" --format json", 
            environment.RootDirectory);

        // assert
        discoverResult.IsSuccess.Should().BeTrue($"chained discover command should succeed. Error: {discoverResult.StandardError}");
        
        // Verify the chained command worked correctly
        var discoverJsonContent = CliProcessExecutor.ExtractJsonFromOutput(discoverResult.StandardOutput);
        var discoverData = JsonSerializer.Deserialize<JsonElement>(discoverJsonContent);
        discoverData.TryGetProperty("data", out var discoverDataContent).Should().BeTrue("Discover JSON should contain data property");
        discoverDataContent.TryGetProperty("templates", out _).Should().BeTrue("Discover JSON data should contain templates property");
    }

    /// <summary>
    /// Tests workflow with large template sets for performance validation
    /// </summary>
    [Fact]
    public async Task CompleteWorkflow_LargeTemplateSet_PerformanceValidation()
    {
        // arrange
        var environment = await CreateLargeTestEnvironmentAsync();
        var replacementMapping = CreateReplacementMapping();
        var mappingFile = Path.Combine(environment.DataDirectory, "replacements.json");
        await File.WriteAllTextAsync(mappingFile, JsonSerializer.Serialize(replacementMapping, new JsonSerializerOptions { WriteIndented = true }));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // act - Execute workflow on large template set
        var largeSetPath = Path.Combine(environment.TemplatesDirectory, "LargeTestSet");
        var copyResult = await _cliExecutor.ExecuteAsync(
            $"copy --source \"{largeSetPath}\" --target \"{environment.OutputDirectory}\"", 
            environment.RootDirectory,
            TimeSpan.FromMinutes(5)); // Extended timeout for large sets

        // Step 2: Replace placeholders in copied files
        var replaceResult = await _cliExecutor.ExecuteAsync(
            $"replace --folder \"{environment.OutputDirectory}\" --map \"{mappingFile}\"", 
            environment.RootDirectory,
            TimeSpan.FromMinutes(5));

        stopwatch.Stop();

        // assert
        copyResult.IsSuccess.Should().BeTrue($"large set copy should succeed. Error: {copyResult.StandardError}");
        replaceResult.IsSuccess.Should().BeTrue($"large set replace should succeed. Error: {replaceResult.StandardError}");
        
        // Performance validation - should complete within reasonable time
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMinutes(3), "large template set processing should complete within 3 minutes");
        
        // Verify all files were processed
        var outputFiles = Directory.GetFiles(environment.OutputDirectory, "*.docx", SearchOption.AllDirectories);
        outputFiles.Length.Should().BeGreaterOrEqualTo(20, "large template set should produce at least 20 output files");
    }

    private async Task<TestEnvironment> CreateTestEnvironmentAsync(string testName)
    {
        var spec = new TestEnvironmentSpec
        {
            Name = testName,
            TemplateSets = new List<TemplateSetSpec>
            {
                new()
                {
                    Name = "TestTemplateSet",
                    DocumentCount = 5,
                    Placeholders = new List<string> { "client_name", "contract_date", "amount", "description" },
                    IncludeCzechCharacters = false
                }
            },
            ReplacementMappings = new List<ReplacementMappingSpec>
            {
                new()
                {
                    Name = "standard",
                    Values = CreateReplacementMapping()
                }
            }
        };

        var environment = await _environmentProvisioner.CreateTestEnvironmentAsync(spec);
        _testEnvironments.Add(environment);
        return environment;
    }

    private async Task<TestEnvironment> CreateTestEnvironmentWithCzechCharactersAsync()
    {
        var spec = new TestEnvironmentSpec
        {
            Name = "CzechCharacterTest",
            TemplateSets = new List<TemplateSetSpec>
            {
                new()
                {
                    Name = "CzechTestSet",
                    DocumentCount = 3,
                    Placeholders = new List<string> { "název", "město", "ulice", "poznámka" },
                    IncludeCzechCharacters = true
                }
            }
        };

        var environment = await _environmentProvisioner.CreateTestEnvironmentAsync(spec);
        _testEnvironments.Add(environment);
        return environment;
    }

    private async Task<TestEnvironment> CreateLargeTestEnvironmentAsync()
    {
        var spec = new TestEnvironmentSpec
        {
            Name = "LargeTemplateSet",
            TemplateSets = new List<TemplateSetSpec>
            {
                new()
                {
                    Name = "LargeTestSet",
                    DocumentCount = 25,
                    Placeholders = new List<string> { "field1", "field2", "field3", "field4", "field5" },
                    IncludeCzechCharacters = false
                }
            }
        };

        var environment = await _environmentProvisioner.CreateTestEnvironmentAsync(spec);
        _testEnvironments.Add(environment);
        return environment;
    }

    private Dictionary<string, string> CreateReplacementMapping()
    {
        return new Dictionary<string, string>
        {
            { "client_name", "Acme Corporation Ltd." },
            { "contract_date", "2025-08-17" },
            { "amount", "$50,000.00" },
            { "description", "Software Development Services" },
            { "field1", "Value 1" },
            { "field2", "Value 2" },
            { "field3", "Value 3" },
            { "field4", "Value 4" },
            { "field5", "Value 5" }
        };
    }

    private Dictionary<string, string> CreateCzechReplacementMapping()
    {
        return new Dictionary<string, string>
        {
            { "název", "Testovací smlouva s českými znaky" },
            { "město", "Brno" },
            { "ulice", "Údolní 53" },
            { "poznámka", "Speciální poznámka s českými znaky: áčďéěíňóřšťúůýž" }
        };
    }

    private async Task ValidateProcessedDocumentsAsync(TestEnvironment environment)
    {
        var outputFiles = Directory.GetFiles(environment.OutputDirectory, "*.docx", SearchOption.AllDirectories);
        
        foreach (var outputFile in outputFiles)
        {
            var originalFile = FindCorrespondingOriginalFile(outputFile, environment);
            if (originalFile != null)
            {
                var validation = await _documentValidator.ValidateDocumentIntegrityAsync(originalFile, outputFile);
                validation.IsValid.Should().BeTrue($"document integrity should be maintained for {outputFile}. Issues: {string.Join(", ", validation.StructureIssues.Concat(validation.CharacterIssues))}");
            }
        }
    }

    private string FindCorrespondingOriginalFile(string outputFile, TestEnvironment environment)
    {
        var outputFileName = Path.GetFileName(outputFile);
        var originalFiles = Directory.GetFiles(environment.TemplatesDirectory, outputFileName, SearchOption.AllDirectories);
        return originalFiles.FirstOrDefault();
    }

    public void Dispose()
    {
        _cliExecutor?.Dispose();
        _environmentProvisioner?.Dispose();
        
        foreach (var environment in _testEnvironments)
        {
            try
            {
                if (Directory.Exists(environment.RootDirectory))
                {
                    Directory.Delete(environment.RootDirectory, true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to cleanup test environment {environment.Name}: {ex.Message}");
            }
        }
    }
}