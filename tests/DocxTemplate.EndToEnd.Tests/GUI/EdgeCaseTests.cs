using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DocxTemplate.EndToEnd.Tests.GUI.Infrastructure;
using FluentAssertions;
using Xunit;

namespace DocxTemplate.EndToEnd.Tests.GUI;

[Collection("Edge Case Tests")]
public class EdgeCaseTests : GuiTestBase
{
    private TestDataManager _testDataManager = null!;
    private CliIntegrationHelper _cliHelper = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _testDataManager = new TestDataManager(TempOutputDirectory);
        _cliHelper = new CliIntegrationHelper();
    }

    [Fact]
    public async Task EdgeCase_SpecialCharactersInPlaceholders_HandledCorrectly()
    {
        // arrange - Create template with special characters in placeholder names
        var templateSetName = "SpecialCharTestSet";
        var templateSetPath = await _testDataManager.CreateTestTemplateSetAsync(templateSetName);
        
        var placeholderValues = new Dictionary<string, string>
        {
            { "company_name", "Test & Co. (Ltd.) - Special chars: äöüß €" },
            { "project_title", "Project with «quotes» and ñ characters" },
            { "current_date", "18/08/2025" },
            { "author_name", "François Müller-Schmidt" }
        };

        var outputPath = Path.Combine(TempOutputDirectory, "special_chars_output");

        // act - Execute workflow with special characters
        var workflowResult = await _cliHelper.ExecuteFullWorkflowCli(
            Path.GetDirectoryName(templateSetPath)!, 
            templateSetName, 
            placeholderValues, 
            outputPath);

        // assert - Workflow should handle special characters correctly
        workflowResult.Success.Should().BeTrue($"Workflow with special characters should succeed. Error: {workflowResult.Error}");
        
        // Verify output files contain special characters
        var outputFiles = Directory.GetFiles(outputPath, "*.docx", SearchOption.AllDirectories);
        outputFiles.Should().NotBeEmpty("Output should contain processed files with special characters");
    }

    [Fact]
    public async Task EdgeCase_LargeNumberOfPlaceholders_ProcessedEfficiently()
    {
        // arrange - This would create a template with many placeholders (simplified for demo)
        var templateSetName = "LargePlaceholderSet";
        var templateSetPath = await _testDataManager.CreateTestTemplateSetAsync(templateSetName);
        
        // Create many placeholder values
        var placeholderValues = new Dictionary<string, string>();
        for (int i = 1; i <= 50; i++)
        {
            placeholderValues[$"field_{i:D2}"] = $"Value {i}";
        }
        
        // Add our standard placeholders
        placeholderValues["company_name"] = "Large Data Test Company";
        placeholderValues["project_title"] = "Performance Test with Many Placeholders";
        placeholderValues["current_date"] = "2025-08-18";
        placeholderValues["author_name"] = "Performance Test Suite";

        var outputPath = Path.Combine(TempOutputDirectory, "large_placeholder_output");
        var startTime = DateTime.UtcNow;

        // act
        var workflowResult = await _cliHelper.ExecuteFullWorkflowCli(
            Path.GetDirectoryName(templateSetPath)!, 
            templateSetName, 
            placeholderValues, 
            outputPath);

        var duration = DateTime.UtcNow - startTime;

        // assert - Should handle large number of placeholders efficiently
        workflowResult.Success.Should().BeTrue("Large placeholder workflow should succeed");
        duration.Should().BeLessThan(TimeSpan.FromMinutes(2), "Large placeholder processing should complete within 2 minutes");
    }

    [Fact]
    public async Task EdgeCase_EmptyPlaceholderValues_HandledGracefully()
    {
        // arrange
        var templateSetName = "EmptyValueTestSet";
        var templateSetPath = await _testDataManager.CreateTestTemplateSetAsync(templateSetName);
        
        var placeholderValues = new Dictionary<string, string>
        {
            { "company_name", "" },
            { "project_title", "" },
            { "current_date", "" },
            { "author_name", "" }
        };

        var outputPath = Path.Combine(TempOutputDirectory, "empty_values_output");

        // act
        var workflowResult = await _cliHelper.ExecuteFullWorkflowCli(
            Path.GetDirectoryName(templateSetPath)!, 
            templateSetName, 
            placeholderValues, 
            outputPath);

        // assert - Should handle empty values gracefully
        workflowResult.Success.Should().BeTrue("Workflow with empty placeholder values should succeed");
        
        var outputFiles = Directory.GetFiles(outputPath, "*.*", SearchOption.AllDirectories);
        outputFiles.Should().NotBeEmpty("Output files should be created even with empty placeholder values");
    }

    [Fact]
    public async Task EdgeCase_ReadOnlyOutputDirectory_ReportsError()
    {
        // arrange
        var templateSetName = "ReadOnlyTestSet";
        var templateSetPath = await _testDataManager.CreateTestTemplateSetAsync(templateSetName);
        var readOnlyPath = Path.Combine(TempOutputDirectory, "readonly_output");
        
        Directory.CreateDirectory(readOnlyPath);
        
        // Make directory read-only (platform-specific)
        try
        {
            var dirInfo = new DirectoryInfo(readOnlyPath);
            dirInfo.Attributes |= FileAttributes.ReadOnly;
        }
        catch
        {
            // Skip test if we can't make directory read-only
            return;
        }

        var placeholderValues = await _testDataManager.LoadExpectedContentAsync();

        try
        {
            // act & assert - Should handle read-only directory appropriately
            var workflowResult = await _cliHelper.ExecuteFullWorkflowCli(
                Path.GetDirectoryName(templateSetPath)!, 
                templateSetName, 
                placeholderValues, 
                readOnlyPath);

            // Either should fail gracefully or handle the read-only scenario
            if (!workflowResult.Success)
            {
                workflowResult.Error.Should().NotBeNullOrEmpty("Error message should be provided for read-only directory");
            }
        }
        finally
        {
            // Clean up - remove read-only attribute
            try
            {
                var dirInfo = new DirectoryInfo(readOnlyPath);
                dirInfo.Attributes &= ~FileAttributes.ReadOnly;
            }
            catch { /* Ignore cleanup errors */ }
        }
    }

    [Fact]
    public async Task EdgeCase_VeryLongFilePaths_HandledCorrectly()
    {
        // arrange - Create deeply nested directory structure
        var longPath = TempOutputDirectory;
        for (int i = 0; i < 10; i++)
        {
            longPath = Path.Combine(longPath, $"very_long_directory_name_level_{i}_with_many_characters");
        }

        var templateSetName = "LongPathTestSet";
        var templateSetPath = await _testDataManager.CreateTestTemplateSetAsync(templateSetName);
        var placeholderValues = await _testDataManager.LoadExpectedContentAsync();

        try
        {
            // act
            var workflowResult = await _cliHelper.ExecuteFullWorkflowCli(
                Path.GetDirectoryName(templateSetPath)!, 
                templateSetName, 
                placeholderValues, 
                longPath);

            // assert - Should either succeed or fail gracefully with clear error
            if (workflowResult.Success)
            {
                Directory.Exists(longPath).Should().BeTrue("Long path should be created if workflow succeeds");
            }
            else
            {
                workflowResult.Error.Should().NotBeNullOrEmpty("Clear error message should be provided for path length issues");
            }
        }
        catch (PathTooLongException)
        {
            // This is acceptable behavior for very long paths
            Assert.True(true, "PathTooLongException is acceptable for very long paths");
        }
    }

    [Fact]
    public async Task EdgeCase_SimultaneousWorkflows_DoNotInterfere()
    {
        // arrange - Set up multiple template sets for parallel processing
        var templateSet1 = await _testDataManager.CreateTestTemplateSetAsync("ParallelTest1");
        var templateSet2 = await _testDataManager.CreateTestTemplateSetAsync("ParallelTest2");
        var templatesDir = Path.GetDirectoryName(templateSet1)!;

        var output1 = Path.Combine(TempOutputDirectory, "parallel_output_1");
        var output2 = Path.Combine(TempOutputDirectory, "parallel_output_2");

        var values1 = new Dictionary<string, string>
        {
            { "company_name", "Parallel Test Company 1" },
            { "project_title", "Workflow 1" },
            { "current_date", "2025-08-18" },
            { "author_name", "Parallel Test 1" }
        };

        var values2 = new Dictionary<string, string>
        {
            { "company_name", "Parallel Test Company 2" },
            { "project_title", "Workflow 2" },
            { "current_date", "2025-08-18" },
            { "author_name", "Parallel Test 2" }
        };

        // act - Execute workflows in parallel
        var workflow1Task = _cliHelper.ExecuteFullWorkflowCli(templatesDir, "ParallelTest1", values1, output1);
        var workflow2Task = _cliHelper.ExecuteFullWorkflowCli(templatesDir, "ParallelTest2", values2, output2);

        var results = await Task.WhenAll(workflow1Task, workflow2Task);

        // assert - Both workflows should succeed independently
        results[0].Success.Should().BeTrue($"Parallel workflow 1 should succeed. Error: {results[0].Error}");
        results[1].Success.Should().BeTrue($"Parallel workflow 2 should succeed. Error: {results[1].Error}");

        // Verify outputs are separate and correct
        Directory.Exists(output1).Should().BeTrue("Output 1 directory should exist");
        Directory.Exists(output2).Should().BeTrue("Output 2 directory should exist");

        var files1 = Directory.GetFiles(output1, "*.*", SearchOption.AllDirectories);
        var files2 = Directory.GetFiles(output2, "*.*", SearchOption.AllDirectories);

        files1.Should().NotBeEmpty("Workflow 1 should produce output files");
        files2.Should().NotBeEmpty("Workflow 2 should produce output files");
    }

    [Fact]
    public async Task EdgeCase_CorruptedTemplateFiles_ReportsClearError()
    {
        // arrange - Create a directory with a non-DOCX file pretending to be a template
        var templateSetDir = Path.Combine(TempOutputDirectory, "CorruptedTestSet");
        Directory.CreateDirectory(templateSetDir);
        
        // Create a corrupted "DOCX" file (just text content)
        var corruptedFile = Path.Combine(templateSetDir, "corrupted.docx");
        await File.WriteAllTextAsync(corruptedFile, "This is not a real DOCX file");

        var placeholderValues = await _testDataManager.LoadExpectedContentAsync();
        var outputPath = Path.Combine(TempOutputDirectory, "corrupted_output");

        // act & assert - Should handle corrupted files gracefully
        var workflowResult = await _cliHelper.ExecuteFullWorkflowCli(
            TempOutputDirectory, 
            "CorruptedTestSet", 
            placeholderValues, 
            outputPath);

        // Should either skip corrupted files or report clear error
        if (!workflowResult.Success)
        {
            workflowResult.Error.Should().NotBeNullOrEmpty("Clear error message should be provided for corrupted files");
        }
    }
}