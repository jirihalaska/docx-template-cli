using DocxTemplate.EndToEnd.Tests.Utilities;
using FluentAssertions;
using System.Text.Json;

namespace DocxTemplate.EndToEnd.Tests.Scenarios;

/// <summary>
/// End-to-end tests for CLI command integration and interactions
/// </summary>
public class CommandIntegrationTests : IDisposable
{
    private readonly CliProcessExecutor _cliExecutor;
    private readonly TestEnvironmentProvisioner _environmentProvisioner;
    private readonly WorkflowStateValidator _workflowValidator;
    private readonly List<TestEnvironment> _testEnvironments = new();

    public CommandIntegrationTests()
    {
        var cliPath = CliProcessExecutor.GetCliExecutablePath();
        _cliExecutor = new CliProcessExecutor(cliPath);
        _environmentProvisioner = new TestEnvironmentProvisioner();
        _workflowValidator = new WorkflowStateValidator();
    }

    /// <summary>
    /// Tests all CLI command combinations and parameter variations
    /// </summary>
    [Fact]
    public async Task AllCommandCombinations_ExecuteSuccessfully()
    {
        // arrange
        var environment = await CreateStandardTestEnvironmentAsync("CommandCombinations");
        var replacementMapping = CreateStandardReplacementMapping();
        var mappingFile = Path.Combine(environment.DataDirectory, "replacements.json");
        await File.WriteAllTextAsync(mappingFile, JsonSerializer.Serialize(replacementMapping, new JsonSerializerOptions { WriteIndented = true }));

        var testCombinations = new[]
        {
            // Basic command variations
            new { Command = $"list-sets --templates \"{environment.TemplatesDirectory}\"", ExpectedSuccess = true },
            new { Command = $"list-sets --templates \"{environment.TemplatesDirectory}\" --output-format json", ExpectedSuccess = true },
            new { Command = $"discover --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\"", ExpectedSuccess = true },
            new { Command = $"discover --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\" --output-format json", ExpectedSuccess = true },
            new { Command = $"scan --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\"", ExpectedSuccess = true },
            new { Command = $"scan --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\" --pattern \"{{{{.*?}}}}\"", ExpectedSuccess = true },
            new { Command = $"copy --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\" --target \"{environment.OutputDirectory}\"", ExpectedSuccess = true },
            new { Command = $"replace --target \"{environment.OutputDirectory}\" --map \"{mappingFile}\"", ExpectedSuccess = true },
            
            // Error conditions
            new { Command = "list-sets --templates \"/nonexistent/path\"", ExpectedSuccess = false },
            new { Command = "discover --set NonExistentSet --templates \"/nonexistent/path\"", ExpectedSuccess = false },
            new { Command = "scan --set NonExistentSet --templates \"/nonexistent/path\"", ExpectedSuccess = false },
            new { Command = "copy --set NonExistentSet --templates \"/nonexistent/path\" --target \"/tmp\"", ExpectedSuccess = false },
            new { Command = "replace --target \"/nonexistent/path\" --map \"/nonexistent/mapping.json\"", ExpectedSuccess = false }
        };

        // act & assert
        foreach (var testCase in testCombinations)
        {
            var result = await _cliExecutor.ExecuteAsync(testCase.Command, environment.RootDirectory);
            
            if (testCase.ExpectedSuccess)
            {
                result.IsSuccess.Should().BeTrue($"Command '{testCase.Command}' should succeed. Error: {result.StandardError}");
            }
            else
            {
                result.IsSuccess.Should().BeFalse($"Command '{testCase.Command}' should fail as expected");
                result.HasError.Should().BeTrue($"Failed command '{testCase.Command}' should provide error message");
            }
        }
    }

    /// <summary>
    /// Tests JSON output format consistency and command interactions
    /// </summary>
    [Fact]
    public async Task JsonOutputFormat_ConsistentAcrossCommands()
    {
        // arrange
        var environment = await CreateStandardTestEnvironmentAsync("JsonConsistency");
        var mappingFile = Path.Combine(environment.DataDirectory, "replacements.json");
        await File.WriteAllTextAsync(mappingFile, JsonSerializer.Serialize(CreateStandardReplacementMapping(), new JsonSerializerOptions { WriteIndented = true }));

        var jsonCommands = new[]
        {
            $"list-sets --templates \"{environment.TemplatesDirectory}\" --output-format json",
            $"discover --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\" --output-format json",
            $"scan --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\" --output-format json",
            $"copy --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\" --target \"{environment.OutputDirectory}\" --output-format json",
            $"replace --target \"{environment.OutputDirectory}\" --map \"{mappingFile}\" --output-format json"
        };

        // act & assert
        foreach (var command in jsonCommands)
        {
            var result = await _cliExecutor.ExecuteAsync(command, environment.RootDirectory);
            
            result.IsSuccess.Should().BeTrue($"JSON command '{command}' should succeed. Error: {result.StandardError}");
            result.HasOutput.Should().BeTrue($"JSON command '{command}' should produce output");
            
            // Validate JSON format
            var jsonValidation = () => JsonSerializer.Deserialize<JsonElement>(result.StandardOutput);
            jsonValidation.Should().NotThrow($"Command '{command}' should produce valid JSON. Output: {result.StandardOutput}");
            
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(result.StandardOutput);
            
            // Common JSON structure validations
            ValidateCommonJsonStructure(jsonElement, command);
        }
    }

    /// <summary>
    /// Tests parameter validation and error scenarios
    /// </summary>
    [Fact]
    public async Task ParameterValidation_ErrorScenariosHandledGracefully()
    {
        // arrange
        var environment = await CreateStandardTestEnvironmentAsync("ParameterValidation");

        var errorScenarios = new[]
        {
            // Missing required parameters
            new { Command = "list-sets", ExpectedError = "templates" },
            new { Command = "discover", ExpectedError = "set" },
            new { Command = "scan", ExpectedError = "set" },
            new { Command = "copy", ExpectedError = "target" },
            new { Command = "replace", ExpectedError = "map" },
            
            // Invalid parameter values
            new { Command = "list-sets --templates \"\"", ExpectedError = "templates" },
            new { Command = $"discover --set \"\" --templates \"{environment.TemplatesDirectory}\"", ExpectedError = "set" },
            new { Command = $"scan --set TestSet --templates \"\" ", ExpectedError = "templates" },
            new { Command = $"copy --set TestSet --templates \"{environment.TemplatesDirectory}\" --target \"\"", ExpectedError = "target" },
            new { Command = $"replace --target \"{environment.OutputDirectory}\" --map \"\"", ExpectedError = "map" },
            
            // Invalid file paths
            new { Command = "replace --target \"/nonexistent\" --map \"/nonexistent.json\"", ExpectedError = "path" }
        };

        // act & assert
        foreach (var scenario in errorScenarios)
        {
            var result = await _cliExecutor.ExecuteAsync(scenario.Command, environment.RootDirectory);
            
            result.IsSuccess.Should().BeFalse($"Command '{scenario.Command}' should fail due to invalid parameters");
            result.HasError.Should().BeTrue($"Failed command '{scenario.Command}' should provide error message");
            result.StandardError.ToLower().Should().Contain(scenario.ExpectedError.ToLower(), 
                $"Error message for '{scenario.Command}' should mention '{scenario.ExpectedError}'. Actual error: {result.StandardError}");
        }
    }

    /// <summary>
    /// Tests command chaining scenarios with data flow validation
    /// </summary>
    [Fact]
    public async Task CommandChaining_DataFlowValidation()
    {
        // arrange
        var environment = await CreateStandardTestEnvironmentAsync("CommandChaining");
        var commands = new List<CliExecutionResult>();

        // act - Execute command chain
        var listResult = await _cliExecutor.ExecuteAsync(
            $"list-sets --templates \"{environment.TemplatesDirectory}\" --output-format json", 
            environment.RootDirectory);
        commands.Add(listResult);

        var discoverResult = await _cliExecutor.ExecuteAsync(
            $"discover --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\" --output-format json", 
            environment.RootDirectory);
        commands.Add(discoverResult);

        var scanResult = await _cliExecutor.ExecuteAsync(
            $"scan --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\" --output-format json", 
            environment.RootDirectory);
        commands.Add(scanResult);

        var copyResult = await _cliExecutor.ExecuteAsync(
            $"copy --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\" --target \"{environment.OutputDirectory}\" --output-format json", 
            environment.RootDirectory);
        commands.Add(copyResult);

        // assert - Validate workflow state
        var workflowExpectation = new WorkflowExpectation
        {
            ExpectedCommandSequence = new List<string> { "list-sets", "discover", "scan", "copy" },
            RequireJsonOutput = new List<string> { "list-sets", "discover", "scan", "copy" },
            RequireTemplateSetConsistency = true,
            AllowTemplateSetChanges = false
        };

        var validationResult = await _workflowValidator.ValidateWorkflowStateAsync(commands, workflowExpectation);
        
        validationResult.IsValid.Should().BeTrue($"Command chaining should maintain valid workflow state. Errors: {string.Join(", ", validationResult.ValidationErrors)}");
        
        // Validate data consistency between commands
        await ValidateDataConsistencyBetweenCommands(commands);
    }

    /// <summary>
    /// Tests output format consistency validation across all commands
    /// </summary>
    [Fact]
    public async Task OutputFormatConsistency_AllCommandsFollowStandards()
    {
        // arrange
        var environment = await CreateStandardTestEnvironmentAsync("OutputConsistency");
        var mappingFile = Path.Combine(environment.DataDirectory, "replacements.json");
        await File.WriteAllTextAsync(mappingFile, JsonSerializer.Serialize(CreateStandardReplacementMapping(), new JsonSerializerOptions { WriteIndented = true }));

        var commandOutputTests = new[]
        {
            new { 
                Command = $"list-sets --templates \"{environment.TemplatesDirectory}\"", 
                ExpectedFormat = "text",
                RequiredElements = new[] { "Template Sets:", "Total sets:" }
            },
            new { 
                Command = $"list-sets --templates \"{environment.TemplatesDirectory}\" --output-format json", 
                ExpectedFormat = "json",
                RequiredElements = new[] { "templateSets", "totalSets" }
            },
            new { 
                Command = $"discover --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\"", 
                ExpectedFormat = "text",
                RequiredElements = new[] { "Templates found:", "Total templates:" }
            },
            new { 
                Command = $"discover --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\" --output-format json", 
                ExpectedFormat = "json",
                RequiredElements = new[] { "templateSet", "templates" }
            }
        };

        // act & assert
        foreach (var test in commandOutputTests)
        {
            var result = await _cliExecutor.ExecuteAsync(test.Command, environment.RootDirectory);
            
            result.IsSuccess.Should().BeTrue($"Command '{test.Command}' should succeed. Error: {result.StandardError}");
            result.HasOutput.Should().BeTrue($"Command '{test.Command}' should produce output");
            
            if (test.ExpectedFormat == "json")
            {
                // Validate JSON format
                var jsonValidation = () => JsonSerializer.Deserialize<JsonElement>(result.StandardOutput);
                jsonValidation.Should().NotThrow($"Command '{test.Command}' should produce valid JSON");
                
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(result.StandardOutput);
                foreach (var requiredElement in test.RequiredElements)
                {
                    jsonElement.TryGetProperty(requiredElement, out _).Should().BeTrue(
                        $"JSON output from '{test.Command}' should contain property '{requiredElement}'. Actual output: {result.StandardOutput}");
                }
            }
            else
            {
                // Validate text format
                foreach (var requiredElement in test.RequiredElements)
                {
                    result.StandardOutput.Should().Contain(requiredElement, 
                        $"Text output from '{test.Command}' should contain '{requiredElement}'. Actual output: {result.StandardOutput}");
                }
            }
        }
    }

    /// <summary>
    /// Tests global options and configuration inheritance
    /// </summary>
    [Fact]
    public async Task GlobalOptions_ConfigurationInheritance()
    {
        // arrange
        var environment = await CreateStandardTestEnvironmentAsync("GlobalOptions");

        var globalOptionTests = new[]
        {
            new { 
                Command = $"list-sets --templates \"{environment.TemplatesDirectory}\" --verbose", 
                ExpectedBehavior = "verbose output"
            },
            new { 
                Command = $"discover --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\" --quiet", 
                ExpectedBehavior = "minimal output"
            },
            new { 
                Command = $"scan --set TestTemplateSet --templates \"{environment.TemplatesDirectory}\" --output-format json", 
                ExpectedBehavior = "json format"
            }
        };

        // act & assert
        foreach (var test in globalOptionTests)
        {
            var result = await _cliExecutor.ExecuteAsync(test.Command, environment.RootDirectory);
            
            result.IsSuccess.Should().BeTrue($"Command with global options '{test.Command}' should succeed. Error: {result.StandardError}");
            
            // Validate specific global option behaviors
            if (test.ExpectedBehavior == "verbose output")
            {
                result.StandardOutput.Length.Should().BeGreaterThan(100, "Verbose output should be detailed");
            }
            else if (test.ExpectedBehavior == "minimal output")
            {
                result.StandardOutput.Length.Should().BeLessThan(500, "Quiet output should be minimal");
            }
            else if (test.ExpectedBehavior == "json format")
            {
                var jsonValidation = () => JsonSerializer.Deserialize<JsonElement>(result.StandardOutput);
                jsonValidation.Should().NotThrow("JSON format option should produce valid JSON");
            }
        }
    }

    private async Task<TestEnvironment> CreateStandardTestEnvironmentAsync(string testName)
    {
        var spec = new TestEnvironmentSpec
        {
            Name = testName,
            TemplateSets = new List<TemplateSetSpec>
            {
                new()
                {
                    Name = "TestTemplateSet",
                    DocumentCount = 3,
                    Placeholders = new List<string> { "client_name", "date", "amount", "description" },
                    IncludeCzechCharacters = false
                }
            },
            ReplacementMappings = new List<ReplacementMappingSpec>
            {
                new()
                {
                    Name = "standard",
                    Values = CreateStandardReplacementMapping()
                }
            }
        };

        var environment = await _environmentProvisioner.CreateTestEnvironmentAsync(spec);
        _testEnvironments.Add(environment);
        return environment;
    }

    private Dictionary<string, string> CreateStandardReplacementMapping()
    {
        return new Dictionary<string, string>
        {
            { "client_name", "Test Corporation" },
            { "date", "2025-08-17" },
            { "amount", "$10,000.00" },
            { "description", "Integration Testing Services" }
        };
    }

    private void ValidateCommonJsonStructure(JsonElement jsonElement, string command)
    {
        // All JSON outputs should have certain common properties
        if (command.Contains("list-sets"))
        {
            jsonElement.TryGetProperty("templateSets", out _).Should().BeTrue("list-sets should have templateSets property");
        }
        else if (command.Contains("discover"))
        {
            jsonElement.TryGetProperty("templateSet", out _).Should().BeTrue("discover should have templateSet property");
            jsonElement.TryGetProperty("templates", out _).Should().BeTrue("discover should have templates property");
        }
        else if (command.Contains("scan"))
        {
            jsonElement.TryGetProperty("placeholders", out _).Should().BeTrue("scan should have placeholders property");
        }
        else if (command.Contains("copy"))
        {
            jsonElement.TryGetProperty("copiedFiles", out _).Should().BeTrue("copy should have copiedFiles property");
        }
        else if (command.Contains("replace"))
        {
            jsonElement.TryGetProperty("replacements", out _).Should().BeTrue("replace should have replacements property");
        }
    }

    private async Task ValidateDataConsistencyBetweenCommands(List<CliExecutionResult> commands)
    {
        var listData = JsonSerializer.Deserialize<JsonElement>(commands[0].StandardOutput);
        var discoverData = JsonSerializer.Deserialize<JsonElement>(commands[1].StandardOutput);
        var scanData = JsonSerializer.Deserialize<JsonElement>(commands[2].StandardOutput);
        var copyData = JsonSerializer.Deserialize<JsonElement>(commands[3].StandardOutput);

        // Validate template set consistency
        var templateSetFromList = listData.GetProperty("templateSets")[0].GetProperty("name").GetString();
        var templateSetFromDiscover = discoverData.GetProperty("templateSet").GetProperty("name").GetString();
        var templateSetFromScan = scanData.GetProperty("templateSet").GetProperty("name").GetString();

        templateSetFromDiscover.Should().Be(templateSetFromList, "Template set should be consistent between list and discover");
        templateSetFromScan.Should().Be(templateSetFromList, "Template set should be consistent between list and scan");

        // Validate template count consistency
        var templatesFromDiscover = discoverData.GetProperty("templates").GetArrayLength();
        var templatesFromCopy = copyData.GetProperty("copiedFiles").GetArrayLength();

        templatesFromCopy.Should().Be(templatesFromDiscover, "Copied file count should match discovered template count");
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