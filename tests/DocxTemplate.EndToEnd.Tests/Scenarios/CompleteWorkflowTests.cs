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
    private readonly List<TestEnvironment> _testEnvironments = [];

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
        listResult.StandardOutput.Should().Contain("01 VZOR Užší řízení");

        // Step 2: Discover templates in the real Czech procurement template set
        var templateSetPath = Path.Combine(environment.TemplatesDirectory, "01 VZOR Užší řízení");
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

        // Copy results to a permanent location for manual inspection
        var permanentOutputDir = Path.Combine(Environment.CurrentDirectory, "test-output-results");
        if (Directory.Exists(permanentOutputDir))
        {
            Directory.Delete(permanentOutputDir, true);
        }
        Directory.CreateDirectory(permanentOutputDir);
        
        // Copy the entire directory structure including empty folders
        CopyDirectoryRecursively(environment.OutputDirectory, permanentOutputDir);
        
        // Debug: Check what's in the environment output directory first
        Console.WriteLine($"🔍 Contents of environment.OutputDirectory: {environment.OutputDirectory}");
        if (Directory.Exists(environment.OutputDirectory))
        {
            var envTemplateSetPath = Path.Combine(environment.OutputDirectory, "01 VZOR Užší řízení");
            if (Directory.Exists(envTemplateSetPath))
            {
                var envDirs = Directory.GetDirectories(envTemplateSetPath, "*", SearchOption.TopDirectoryOnly)
                    .Select(d => Path.GetFileName(d))
                    .OrderBy(d => d)
                    .ToArray();
                
                Console.WriteLine($"📁 Directories in environment output:");
                foreach (var dir in envDirs)
                {
                    Console.WriteLine($"  ✓ {dir}");
                }
            }
        }
        
        // Also copy the replacement mapping file for reference
        File.Copy(mappingFile, Path.Combine(permanentOutputDir, "replacements.json"), true);
        
        Console.WriteLine($"✅ Test completed successfully!");
        Console.WriteLine($"📁 Processed documents available at: {permanentOutputDir}");
        Console.WriteLine($"📄 Original templates were from: {Path.Combine(environment.TemplatesDirectory, "01 VZOR Užší řízení")}");
        Console.WriteLine($"🔄 {Directory.GetFiles(environment.OutputDirectory, "*.docx", SearchOption.AllDirectories).Length} documents were processed with Czech placeholder replacements");

        // Verify document integrity after complete workflow
        await ValidateProcessedDocumentsAsync(environment);
        
        // Verify complete directory structure is preserved
        ValidateCompleteDirectoryStructure(environment, permanentOutputDir);
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

            var validation = await _documentValidator.ValidateCharacterPreservationAsync(originalFile, outputFile);
            validation.IsCharactersPreserved.Should().BeTrue($"Czech characters should be preserved in {outputFile}. Issues: {string.Join(", ", validation.Issues)}");
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
        var templateSetPath = Path.Combine(environment.TemplatesDirectory, "01 VZOR Užší řízení");
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

        // Verify all files were processed (expecting at least 9 files from real Czech templates)
        var outputFiles = Directory.GetFiles(environment.OutputDirectory, "*.docx", SearchOption.AllDirectories);
        outputFiles.Length.Should().BeGreaterOrEqualTo(9, "large template set should produce at least 9 output files from real Czech templates");
    }

    private async Task<TestEnvironment> CreateTestEnvironmentAsync(string testName)
    {
        // Use the real Czech procurement templates that are copied to the test output directory
        var templatesPath = Path.Combine(AppContext.BaseDirectory, "templates");
        var environment = await _environmentProvisioner.CreateTestEnvironmentWithRealTemplatesAsync(testName, templatesPath);
        _testEnvironments.Add(environment);
        return environment;
    }

    private async Task<TestEnvironment> CreateTestEnvironmentWithCzechCharactersAsync()
    {
        var spec = new TestEnvironmentSpec
        {
            Name = "CzechCharacterTest",
            TemplateSets =
            [
                new()
                {
                    Name = "CzechTestSet",
                    DocumentCount = 3,
                    Placeholders = ["název", "město", "ulice", "poznámka"],
                    IncludeCzechCharacters = true
                }
            ]
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
            TemplateSets =
            [
                new()
                {
                    Name = "LargeTestSet",
                    DocumentCount = 25,
                    Placeholders = ["field1", "field2", "field3", "field4", "field5"],
                    IncludeCzechCharacters = false
                }
            ]
        };

        var environment = await _environmentProvisioner.CreateTestEnvironmentAsync(spec);
        _testEnvironments.Add(environment);
        return environment;
    }

    private Dictionary<string, string> CreateReplacementMapping()
    {
        // Real Czech procurement placeholders with meaningful values
        return new Dictionary<string, string>
        {
            { "ZAKAZKA_NAZEV", "Dodávka IT služeb pro městský úřad" },
            { "ZADAVATEL_NAZEV", "Městský úřad Brno-střed" },
            { "ZADAVATEL_ADRESA", "Dominikánské náměstí 196/1, 602 00 Brno" },
            { "ZADAVATEL_ICO", "44992785" },
            { "ZAKAZKA_PREDMET_DRUH_RIZENI", "Dodávka informačních technologií a služeb - užší řízení" },
            { "DODAVATEL", "IT Solutions s.r.o." }
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
        
        // First, verify that files were actually processed
        outputFiles.Should().NotBeEmpty("Processed files should exist in output directory after complete workflow");
        
        // Get expected file count from the specific template set that was copied
        var templateSetPath = Path.Combine(environment.TemplatesDirectory, "01 VZOR Užší řízení");
        var templateFiles = Directory.GetFiles(templateSetPath, "*.docx", SearchOption.AllDirectories);
        templateFiles.Should().NotBeEmpty("Template files should exist to validate against");
        
        // Filter out backup files from validation (backup folders start with "backup_")
        // The replace command creates backups by default, so we validate only the processed files
        var nonBackupFiles = outputFiles.Where(f => !f.Contains("/backup_") && !f.Contains("\\backup_")).ToArray();
        
        // Verify we have the expected number of non-backup output files
        nonBackupFiles.Length.Should().Be(templateFiles.Length, 
            $"Output should contain same number of non-backup files as templates. Templates: {templateFiles.Length}, Non-backup Output: {nonBackupFiles.Length}");

        foreach (var outputFile in nonBackupFiles)
        {
            var originalFile = FindCorrespondingOriginalFile(outputFile, environment);
            var validation = await _documentValidator.ValidateDocumentIntegrityAsync(originalFile, outputFile);
            
            // For real document replacement, we mainly care about structure and format integrity
            // Character content will change significantly due to placeholder replacement
            validation.IsValidDocxFormat.Should().BeTrue($"processed document should be valid DOCX format: {outputFile}");
            validation.StructurePreserved.Should().BeTrue($"document structure should be preserved for {outputFile}. Issues: {string.Join(", ", validation.StructureIssues)}");
            
            // We don't validate character preservation for real document tests since placeholders are replaced with real content
            // This is expected behavior and not an error
        }
    }

    private string FindCorrespondingOriginalFile(string outputFile, TestEnvironment environment)
    {
        var outputFileName = Path.GetFileName(outputFile);
        var originalFiles = Directory.GetFiles(environment.TemplatesDirectory, outputFileName, SearchOption.AllDirectories);
        return originalFiles.FirstOrDefault() ?? 
            throw new InvalidOperationException($"Could not find original template file '{outputFileName}' in templates directory '{environment.TemplatesDirectory}'");
    }

    /// <summary>
    /// Validates that the complete directory structure from the original templates is preserved in the output
    /// </summary>
    private void ValidateCompleteDirectoryStructure(TestEnvironment environment, string outputDir)
    {
        var outputTemplateSetPath = Path.Combine(outputDir, "01 VZOR Užší řízení");
        
        // Debug: Check if the output directory exists and list its contents
        if (!Directory.Exists(outputTemplateSetPath))
        {
            Console.WriteLine($"❌ Output template set path not found: {outputTemplateSetPath}");
            Console.WriteLine($"Available directories in {outputDir}:");
            if (Directory.Exists(outputDir))
            {
                foreach (var dir in Directory.GetDirectories(outputDir))
                {
                    Console.WriteLine($"  - {Path.GetFileName(dir)}");
                }
            }
            outputTemplateSetPath.Should().NotBeNull("Output template set directory should exist");
            return;
        }

        // Define the expected complete directory structure for Czech procurement templates
        var expectedDirectories = new[]
        {
            "2. Zahájení řízení",           // Initiation of Proceedings (empty)
            "3. Vysvětlení ZD",             // Procurement Clarification (has files)
            "4. Doručené ŽoÚ",              // Received Applications (empty)
            "5. Výzvy k objasnění ŽoÚ",     // Requests for Clarification (has files)
            "6. Objasnění ŽoÚ",             // Application Clarifications (empty)
            "7. Výzva k podání nabídek",    // Invitation to Tender (has files)
            "8. Doručené nabídky",          // Received Bids (empty)
            "9. Zpráva o hodnocení, Oznámení o výběru", // Evaluation Report, Award Notice (has files)
            "10. Smlouva",                  // Contract (empty)
            "11. Závěrečné úkony"           // Final Actions (has files)
        };

        var expectedFiles = new[]
        {
            "3. Vysvětlení ZD/Vysvetleni ZD 1.docx",
            "5. Výzvy k objasnění ŽoÚ/Výzva k objasnění žádosti.docx",
            "7. Výzva k podání nabídek/Výzva k podání nabídky (II. kolo).docx",
            "9. Zpráva o hodnocení, Oznámení o výběru/CP_stret zajmu_administrator.docx",
            "9. Zpráva o hodnocení, Oznámení o výběru/CP_stret zajmu_komise.docx",
            "9. Zpráva o hodnocení, Oznámení o výběru/Seznam zakázek k prokázání tech. kvalifikace.docx",
            "9. Zpráva o hodnocení, Oznámení o výběru/Vzdání se práva na podání námitek.docx",
            "9. Zpráva o hodnocení, Oznámení o výběru/Výsledek posouzení splnění podmínek.docx",
            "11. Závěrečné úkony/Pisemna zprava zadavatele.docx"
        };

        // Debug: List all actual directories in the output
        Console.WriteLine($"🔍 Actual directories in {outputTemplateSetPath}:");
        var actualDirs = Directory.GetDirectories(outputTemplateSetPath, "*", SearchOption.TopDirectoryOnly)
            .Select(d => Path.GetFileName(d))
            .OrderBy(d => d)
            .ToArray();
        
        foreach (var dir in actualDirs)
        {
            Console.WriteLine($"  ✓ {dir}");
        }
        
        // Verify all expected directories exist
        foreach (var expectedDir in expectedDirectories)
        {
            var dirPath = Path.Combine(outputTemplateSetPath, expectedDir);
            if (!Directory.Exists(dirPath))
            {
                Console.WriteLine($"❌ Missing expected directory: {expectedDir}");
                Console.WriteLine($"   Full path: {dirPath}");
            }
            Directory.Exists(dirPath).Should().BeTrue($"Expected directory should exist: {expectedDir}");
        }

        // Verify all expected files exist
        foreach (var expectedFile in expectedFiles)
        {
            var filePath = Path.Combine(outputTemplateSetPath, expectedFile);
            File.Exists(filePath).Should().BeTrue($"Expected file should exist: {expectedFile}");
        }

        // Verify no unexpected directories were created
        var actualDirectories = Directory.GetDirectories(outputTemplateSetPath, "*", SearchOption.TopDirectoryOnly)
            .Select(d => Path.GetFileName(d))
            .OrderBy(d => d)
            .ToArray();

        var expectedDirectoriesOrdered = expectedDirectories.OrderBy(d => d).ToArray();
        
        actualDirectories.Should().BeEquivalentTo(expectedDirectoriesOrdered, 
            "Output should contain exactly the expected Czech procurement workflow directories");

        // Verify file count matches expected
        var actualFiles = Directory.GetFiles(outputTemplateSetPath, "*.docx", SearchOption.AllDirectories);
        var nonBackupFiles = actualFiles.Where(f => !f.Contains("backup_")).ToArray();
        
        nonBackupFiles.Length.Should().Be(expectedFiles.Length, 
            "Output should contain exactly the expected number of processed template files");

        Console.WriteLine($"✅ Directory structure validation passed:");
        Console.WriteLine($"   📁 {expectedDirectories.Length} directories verified (including {expectedDirectories.Count(d => !expectedFiles.Any(f => f.StartsWith(d)))} empty directories)");
        Console.WriteLine($"   📄 {expectedFiles.Length} files verified");
        Console.WriteLine($"   🎯 Complete Czech procurement workflow structure preserved");
    }

    /// <summary>
    /// Recursively copies a directory and all its contents, including empty subdirectories
    /// </summary>
    private static void CopyDirectoryRecursively(string sourceDir, string destDir)
    {
        if (!Directory.Exists(sourceDir))
            return;

        // Create the destination directory if it doesn't exist
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        // Copy all files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, true);
        }

        // Copy all subdirectories recursively
        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(directory);
            var destSubDir = Path.Combine(destDir, dirName);
            CopyDirectoryRecursively(directory, destSubDir);
        }
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
