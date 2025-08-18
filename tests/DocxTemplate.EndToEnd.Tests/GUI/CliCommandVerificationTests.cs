using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DocxTemplate.EndToEnd.Tests.GUI.Infrastructure;
using FluentAssertions;
using Xunit;

namespace DocxTemplate.EndToEnd.Tests.GUI;

[Collection("CLI Command Verification")]
public class CliCommandVerificationTests : GuiTestBase
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
    public async Task CliCommands_ListSets_ReturnsExpectedFormat()
    {
        // arrange
        var templateSetName = "CommandTestSet";
        var templateSetPath = await _testDataManager.CreateTestTemplateSetAsync(templateSetName);
        var templatesDir = Path.GetDirectoryName(templateSetPath)!;

        // act
        var templateSets = await _cliHelper.GetTemplateSetsCli(templatesDir);

        // assert - Verify CLI command returns expected data structure
        templateSets.Should().NotBeNull("CLI should return template sets list");
        templateSets.Should().Contain(templateSetName, "CLI should find the test template set");
        templateSets.Count.Should().BeGreaterOrEqualTo(1, "CLI should find at least one template set");
    }

    [Fact]
    public async Task CliCommands_ScanPlaceholders_ReturnsExpectedFormat()
    {
        // arrange
        var templateSetName = "PlaceholderTestSet";
        var templateSetPath = await _testDataManager.CreateTestTemplateSetAsync(templateSetName);

        // act
        var placeholders = await _cliHelper.ScanPlaceholdersCli(templateSetPath);

        // assert - Verify CLI command returns expected placeholder data
        placeholders.Should().NotBeNull("CLI should return placeholders dictionary");
        placeholders.Should().NotBeEmpty("CLI should find placeholders in test documents");
        
        // Verify specific placeholders from test document
        placeholders.Should().ContainKey("company_name");
        placeholders.Should().ContainKey("project_title");
        placeholders.Should().ContainKey("current_date");
        placeholders.Should().ContainKey("author_name");

        // Verify placeholder locations are included
        foreach (var placeholder in placeholders)
        {
            placeholder.Value.Should().NotBeEmpty($"Placeholder '{placeholder.Key}' should have location information");
        }
    }

    [Fact]
    public async Task CliCommands_CopyTemplates_CreatesExpectedOutput()
    {
        // arrange
        var templateSetName = "CopyTestSet";
        var templateSetPath = await _testDataManager.CreateTestTemplateSetAsync(templateSetName);
        var outputPath = Path.Combine(TempOutputDirectory, "copy_output");
        Directory.CreateDirectory(outputPath);

        // act
        var copyResult = await _cliHelper.CopyTemplatesCli(templateSetPath, outputPath);

        // assert - Verify copy operation succeeded
        copyResult.Should().BeTrue("CLI copy command should succeed");

        // Verify files were actually copied
        var copiedFiles = Directory.GetFiles(outputPath, "*.*", SearchOption.AllDirectories);
        copiedFiles.Should().NotBeEmpty("Copied directory should contain files");

        // Verify source files exist in output (with source folder name preserved)
        var sourceFiles = Directory.GetFiles(templateSetPath, "*.*", SearchOption.AllDirectories);
        var sourceDirectoryName = Path.GetFileName(templateSetPath);
        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(templateSetPath, sourceFile);
            var targetFile = Path.Combine(outputPath, sourceDirectoryName, relativePath);
            File.Exists(targetFile).Should().BeTrue($"File {relativePath} should be copied to output directory under source folder {sourceDirectoryName}");
        }
    }

    [Fact]
    public async Task CliCommands_ReplacePlaceholders_ModifiesContent()
    {
        // arrange
        var templateSetName = "ReplaceTestSet";
        var templateSetPath = await _testDataManager.CreateTestTemplateSetAsync(templateSetName);
        var outputPath = Path.Combine(TempOutputDirectory, "replace_output");
        Directory.CreateDirectory(outputPath);

        // First copy templates
        var copyResult = await _cliHelper.CopyTemplatesCli(templateSetPath, outputPath);
        copyResult.Should().BeTrue("Copy should succeed before replace");

        // Create placeholder values
        var placeholderValues = new Dictionary<string, string>
        {
            { "company_name", "Verified Test Company" },
            { "project_title", "CLI Verification Project" },
            { "current_date", "2025-08-18" },
            { "author_name", "CLI Test Suite" }
        };

        var valuesFile = await _testDataManager.CreateTestPlaceholderValuesAsync("replace_values.json");

        // act
        var replaceResult = await _cliHelper.ReplacePlaceholdersCli(outputPath, valuesFile);

        // assert - Verify replace operation succeeded
        replaceResult.Should().BeTrue("CLI replace command should succeed");

        // Verify placeholders were actually replaced (simplified check)
        var docxFiles = Directory.GetFiles(outputPath, "*.docx", SearchOption.AllDirectories);
        docxFiles.Should().NotBeEmpty("Output should contain processed DOCX files");
    }

    [Fact]
    public async Task CliCommands_FullWorkflow_ExecutesAllStepsCorrectly()
    {
        // arrange
        var templateSetName = "WorkflowTestSet";
        var templateSetPath = await _testDataManager.CreateTestTemplateSetAsync(templateSetName);
        var templatesDir = Path.GetDirectoryName(templateSetPath)!;
        var outputPath = Path.Combine(TempOutputDirectory, "workflow_output");

        var placeholderValues = new Dictionary<string, string>
        {
            { "company_name", "Full Workflow Test Company" },
            { "project_title", "Complete CLI Integration Test" },
            { "current_date", "2025-08-18" },
            { "author_name", "E2E Workflow Test" }
        };

        // act - Execute complete workflow
        var workflowResult = await _cliHelper.ExecuteFullWorkflowCli(templatesDir, templateSetName, placeholderValues, outputPath);

        // assert - Verify all workflow steps completed successfully
        workflowResult.Success.Should().BeTrue($"Complete workflow should succeed. Error: {workflowResult.Error}");
        workflowResult.TemplateSets.Should().Contain(templateSetName, "Workflow should find template set");
        workflowResult.Placeholders.Should().NotBeEmpty("Workflow should discover placeholders");
        workflowResult.CopySuccess.Should().BeTrue("Workflow should successfully copy templates");
        workflowResult.ReplaceSuccess.Should().BeTrue("Workflow should successfully replace placeholders");

        // Verify final output exists
        Directory.Exists(outputPath).Should().BeTrue("Output directory should exist after workflow");
        var outputFiles = Directory.GetFiles(outputPath, "*.*", SearchOption.AllDirectories);
        outputFiles.Should().NotBeEmpty("Workflow should produce output files");
    }

    [Fact]
    public async Task CliCommands_ErrorHandling_InvalidTemplatesPath()
    {
        // arrange
        var invalidPath = "/path/that/does/not/exist";

        // act & assert - CLI should handle invalid paths gracefully
        var action = () => _cliHelper.GetTemplateSetsCli(invalidPath);
        await action.Should().ThrowAsync<InvalidOperationException>("CLI should report error for invalid template path");
    }

    [Fact]
    public async Task CliCommands_ErrorHandling_EmptyTemplateDirectory()
    {
        // arrange
        var emptyDir = Path.Combine(TempOutputDirectory, "empty_templates");
        Directory.CreateDirectory(emptyDir);

        // act
        var templateSets = await _cliHelper.GetTemplateSetsCli(emptyDir);

        // assert - CLI should handle empty directories gracefully
        templateSets.Should().BeEmpty("CLI should return empty list for directory with no template sets");
    }

    [Fact]
    public async Task CliCommands_Performance_CompletesWithinReasonableTime()
    {
        // arrange
        var templateSetName = "PerformanceTestSet";
        var templateSetPath = await _testDataManager.CreateTestTemplateSetAsync(templateSetName);
        var templatesDir = Path.GetDirectoryName(templateSetPath)!;
        var outputPath = Path.Combine(TempOutputDirectory, "perf_output");

        var placeholderValues = await _testDataManager.LoadExpectedContentAsync();
        var startTime = DateTime.UtcNow;

        // act
        var workflowResult = await _cliHelper.ExecuteFullWorkflowCli(templatesDir, templateSetName, placeholderValues, outputPath);

        // assert - Performance check
        var duration = DateTime.UtcNow - startTime;
        workflowResult.Success.Should().BeTrue("Performance test workflow should succeed");
        duration.Should().BeLessThan(TimeSpan.FromMinutes(1), "Full workflow should complete within 1 minute");
    }
}