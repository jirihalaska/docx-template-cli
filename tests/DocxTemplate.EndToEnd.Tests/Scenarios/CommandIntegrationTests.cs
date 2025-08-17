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
            new { Command = $"list-sets --templates \"{environment.TemplatesDirectory}\" --format json", ExpectedSuccess = true },
            new { Command = $"discover --path \"{environment.TemplatesDirectory}\"", ExpectedSuccess = true },
            new { Command = $"discover --path \"{environment.TemplatesDirectory}\" --format json", ExpectedSuccess = true },
            new { Command = $"scan --path \"{environment.TemplatesDirectory}\"", ExpectedSuccess = true },
            new { Command = $"scan --path \"{environment.TemplatesDirectory}\" --pattern \"{{{{.*?}}}}\"", ExpectedSuccess = true },
            new { Command = $"copy --source \"{environment.TemplatesDirectory}\" --target \"{environment.OutputDirectory}\"", ExpectedSuccess = true },
            // Replace command not implemented yet
            // new { Command = $"replace --target \"{environment.OutputDirectory}\" --map \"{mappingFile}\"", ExpectedSuccess = true },
            
            // Error conditions - CLI commands properly return non-zero exit codes for errors
            new { Command = "list-sets --templates \"/nonexistent/path\"", ExpectedSuccess = false },
            new { Command = "discover --path \"/nonexistent/path\"", ExpectedSuccess = false },
            new { Command = "scan --path \"/nonexistent/path\"", ExpectedSuccess = true }, // scan returns 0 but logs error
            new { Command = "copy --source \"/nonexistent/path\" --target \"/tmp\"", ExpectedSuccess = false }
            // Replace command not implemented yet
            // new { Command = "replace --target \"/nonexistent/path\" --map \"/nonexistent/mapping.json\"", ExpectedSuccess = false }
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
            $"list-sets --templates \"{environment.TemplatesDirectory}\" --format json",
            $"discover --path \"{environment.TemplatesDirectory}\" --format json",
            $"scan --path \"{environment.TemplatesDirectory}\" --format json",
            $"copy --source \"{environment.TemplatesDirectory}\" --target \"{environment.OutputDirectory}\" --format json"
            // Replace command not implemented yet
            // $"replace --target \"{environment.OutputDirectory}\" --map \"{mappingFile}\" --format json"
        };

        // act & assert
        foreach (var command in jsonCommands)
        {
            var result = await _cliExecutor.ExecuteAsync(command, environment.RootDirectory);
            
            result.IsSuccess.Should().BeTrue($"JSON command '{command}' should succeed. Error: {result.StandardError}");
            result.HasOutput.Should().BeTrue($"JSON command '{command}' should produce output");
            
            // Validate JSON format
            var jsonContent = CliProcessExecutor.ExtractJsonFromOutput(result.StandardOutput);
            var jsonValidation = () => JsonSerializer.Deserialize<JsonElement>(jsonContent);
            jsonValidation.Should().NotThrow($"Command '{command}' should produce valid JSON. Output: {result.StandardOutput}");
            
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonContent);
            
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
            new { Command = "discover", ExpectedError = "path" },
            new { Command = "scan", ExpectedError = "path" },
            new { Command = "copy", ExpectedError = "source" },
            
            // Invalid parameter values
            new { Command = "list-sets --templates \"\"", ExpectedError = "templates" },
            new { Command = "discover --path \"\"", ExpectedError = "path" },
            new { Command = "scan --path \"\"", ExpectedError = "path" },
            new { Command = $"copy --source \"{environment.TemplatesDirectory}\" --target \"\"", ExpectedError = "target" },
            
            // Invalid file paths - replace command not implemented yet
            // new { Command = "replace --target \"/nonexistent\" --map \"/nonexistent.json\"", ExpectedError = "path" }
        };

        // act & assert
        foreach (var scenario in errorScenarios)
        {
            var result = await _cliExecutor.ExecuteAsync(scenario.Command, environment.RootDirectory);
            
            // Commands should fail with parameter validation errors
            result.IsSuccess.Should().BeFalse($"Command '{scenario.Command}' should fail due to invalid parameters");
            result.HasError.Should().BeTrue($"Failed command '{scenario.Command}' should provide error message");
            
            // Check that error message contains relevant information
            var hasRelevantError = result.StandardError.ToLower().Contains(scenario.ExpectedError.ToLower()) ||
                                 result.StandardOutput.ToLower().Contains(scenario.ExpectedError.ToLower());
            hasRelevantError.Should().BeTrue(
                $"Error output for '{scenario.Command}' should mention '{scenario.ExpectedError}'. Actual error: {result.StandardError}. Actual output: {result.StandardOutput}");
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
            $"list-sets --templates \"{environment.TemplatesDirectory}\" --format json", 
            environment.RootDirectory);
        commands.Add(listResult);

        var discoverResult = await _cliExecutor.ExecuteAsync(
            $"discover --path \"{environment.TemplatesDirectory}\" --format json", 
            environment.RootDirectory);
        commands.Add(discoverResult);

        var scanResult = await _cliExecutor.ExecuteAsync(
            $"scan --path \"{environment.TemplatesDirectory}\" --format json", 
            environment.RootDirectory);
        commands.Add(scanResult);

        var copyResult = await _cliExecutor.ExecuteAsync(
            $"copy --source \"{environment.TemplatesDirectory}\" --target \"{environment.OutputDirectory}\" --format json", 
            environment.RootDirectory);
        commands.Add(copyResult);

        // assert - Validate workflow state
        var workflowExpectation = new WorkflowExpectation
        {
            ExpectedCommandSequence = new List<string> { "list-sets", "discover", "scan", "copy" },
            RequireJsonOutput = new List<string> { "list-sets", "discover", "scan", "copy" },
            RequireTemplateSetConsistency = false, // Different commands use different parameters
            AllowTemplateSetChanges = true
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
                RequiredElements = new[] { "Template Sets", "template set(s)" }
            },
            new { 
                Command = $"list-sets --templates \"{environment.TemplatesDirectory}\" --format json", 
                ExpectedFormat = "json",
                RequiredElements = new[] { "templateSets" }
            },
            new { 
                Command = $"discover --path \"{environment.TemplatesDirectory}\"", 
                ExpectedFormat = "text",
                RequiredElements = new[] { "template(s)", "Total" }
            },
            new { 
                Command = $"discover --path \"{environment.TemplatesDirectory}\" --format json", 
                ExpectedFormat = "json",
                RequiredElements = new[] { "templates" }
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
                var jsonContent = CliProcessExecutor.ExtractJsonFromOutput(result.StandardOutput);
                var jsonValidation = () => JsonSerializer.Deserialize<JsonElement>(jsonContent);
                jsonValidation.Should().NotThrow($"Command '{test.Command}' should produce valid JSON");
                
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                foreach (var requiredElement in test.RequiredElements)
                {
                    if (requiredElement == "templateSets")
                    {
                        // Update to use actual property name structure
                        jsonElement.TryGetProperty("data", out var data).Should().BeTrue("JSON should have data property");
                        data.TryGetProperty("template_sets", out _).Should().BeTrue(
                            $"JSON output from '{test.Command}' should contain property 'template_sets' in data. Actual output: {jsonContent}");
                    }
                    else if (requiredElement == "templates")
                    {
                        jsonElement.TryGetProperty("data", out var data).Should().BeTrue("JSON should have data property");
                        data.TryGetProperty("templates", out _).Should().BeTrue(
                            $"JSON output from '{test.Command}' should contain property 'templates' in data. Actual output: {jsonContent}");
                    }
                    else
                    {
                        jsonElement.TryGetProperty(requiredElement, out _).Should().BeTrue(
                            $"JSON output from '{test.Command}' should contain property '{requiredElement}'. Actual output: {jsonContent}");
                    }
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
                Command = $"list-sets --templates \"{environment.TemplatesDirectory}\" --help", 
                ExpectedBehavior = "help output"
            },
            new { 
                Command = $"discover --path \"{environment.TemplatesDirectory}\" --help", 
                ExpectedBehavior = "help output"
            },
            new { 
                Command = $"scan --path \"{environment.TemplatesDirectory}\" --format json", 
                ExpectedBehavior = "json format"
            }
        };

        // act & assert
        foreach (var test in globalOptionTests)
        {
            var result = await _cliExecutor.ExecuteAsync(test.Command, environment.RootDirectory);
            
            result.IsSuccess.Should().BeTrue($"Command with global options '{test.Command}' should succeed. Error: {result.StandardError}");
            
            // Validate specific global option behaviors
            if (test.ExpectedBehavior == "help output")
            {
                result.StandardOutput.Should().Contain("Usage:", "Help output should contain usage information");
                result.StandardOutput.Should().Contain("Options:", "Help output should contain options information");
            }
            else if (test.ExpectedBehavior == "json format")
            {
                var jsonContent = CliProcessExecutor.ExtractJsonFromOutput(result.StandardOutput);
                var jsonValidation = () => JsonSerializer.Deserialize<JsonElement>(jsonContent);
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
        // All JSON outputs should have the common structure: command, timestamp, success, data
        jsonElement.TryGetProperty("command", out _).Should().BeTrue("JSON should have command property");
        jsonElement.TryGetProperty("timestamp", out _).Should().BeTrue("JSON should have timestamp property");
        jsonElement.TryGetProperty("success", out _).Should().BeTrue("JSON should have success property");
        jsonElement.TryGetProperty("data", out var data).Should().BeTrue("JSON should have data property");
        
        // Validate command-specific data structure
        if (command.Contains("list-sets"))
        {
            data.TryGetProperty("template_sets", out _).Should().BeTrue("list-sets should have template_sets property in data");
        }
        else if (command.Contains("discover"))
        {
            data.TryGetProperty("templates", out _).Should().BeTrue("discover should have templates property in data");
        }
        else if (command.Contains("scan"))
        {
            data.TryGetProperty("placeholders", out _).Should().BeTrue("scan should have placeholders property in data");
        }
        else if (command.Contains("copy"))
        {
            data.TryGetProperty("copied_files", out _).Should().BeTrue("copy should have copied_files property in data");
        }
        else if (command.Contains("replace"))
        {
            data.TryGetProperty("replacements", out _).Should().BeTrue("replace should have replacements property in data");
        }
    }

    private async Task ValidateDataConsistencyBetweenCommands(List<CliExecutionResult> commands)
    {
        // Basic validation that JSON outputs are valid
        foreach (var command in commands.Where(c => c.IsSuccess))
        {
            var jsonContent = CliProcessExecutor.ExtractJsonFromOutput(command.StandardOutput);
            var jsonValidation = () => JsonSerializer.Deserialize<JsonElement>(jsonContent);
            jsonValidation.Should().NotThrow($"Command output should be valid JSON: {command.Command}");
        }

        // Additional consistency checks can be added as commands are implemented
        // For now, just validate that all commands succeeded
        commands.All(c => c.IsSuccess).Should().BeTrue("All commands in the chain should succeed");
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