using DocxTemplate.EndToEnd.Tests.Utilities;
using FluentAssertions;
using System.Text.Json;

namespace DocxTemplate.EndToEnd.Tests.Scenarios;

/// <summary>
/// Tests to validate CLI documentation accuracy against actual implementation
/// </summary>
public class CliDocumentationValidationTests : IDisposable
{
    private readonly CliProcessExecutor _cliExecutor;
    private readonly TestEnvironmentProvisioner _environmentProvisioner;
    private readonly List<TestEnvironment> _testEnvironments = [];

    public CliDocumentationValidationTests()
    {
        var cliPath = CliProcessExecutor.GetCliExecutablePath();
        _cliExecutor = new CliProcessExecutor(cliPath);
        _environmentProvisioner = new TestEnvironmentProvisioner();
    }

    /// <summary>
    /// Tests that all documented commands exist and accept their documented parameters
    /// </summary>
    [Fact]
    public async Task DocumentedCommands_AllParametersAccepted()
    {
        // arrange
        var environment = await CreateTestEnvironmentAsync("ParameterValidation");
        var mappingFile = Path.Combine(environment.DataDirectory, "replacements.json");
        await File.WriteAllTextAsync(mappingFile, JsonSerializer.Serialize(new Dictionary<string, string> {
            ["{{CLIENT_NAME}}"] = "Test Corporation",
            ["{{DATE}}"] = "2025-08-18"
        }));

        // Copy templates for replace command
        await _cliExecutor.ExecuteAsync($"copy --source \"{environment.TemplatesDirectory}\" --target \"{environment.OutputDirectory}\"", environment.RootDirectory);

        var documentedParameterTests = new[]
        {
            // list-sets command parameters
            new { Command = $"list-sets --templates \"{environment.TemplatesDirectory}\"", ShouldSucceed = true },
            new { Command = $"list-sets --templates \"{environment.TemplatesDirectory}\" --format json", ShouldSucceed = true },
            new { Command = $"list-sets --templates \"{environment.TemplatesDirectory}\" --format text", ShouldSucceed = true },
            new { Command = $"list-sets --templates \"{environment.TemplatesDirectory}\" --details", ShouldSucceed = true },
            new { Command = $"list-sets --templates \"{environment.TemplatesDirectory}\" --include-empty", ShouldSucceed = true },

            // discover command parameters
            new { Command = $"discover --path \"{environment.TemplatesDirectory}\"", ShouldSucceed = true },
            new { Command = $"discover --path \"{environment.TemplatesDirectory}\" --recursive", ShouldSucceed = true },
            new { Command = $"discover --path \"{environment.TemplatesDirectory}\" --format json", ShouldSucceed = true },
            new { Command = $"discover --path \"{environment.TemplatesDirectory}\" --include \"*.docx\"", ShouldSucceed = true },
            new { Command = $"discover --path \"{environment.TemplatesDirectory}\" --max-depth 2", ShouldSucceed = true },
            new { Command = $"discover --path \"{environment.TemplatesDirectory}\" --min-size 1", ShouldSucceed = true },
            new { Command = $"discover --path \"{environment.TemplatesDirectory}\" --quiet", ShouldSucceed = true },

            // scan command parameters
            new { Command = $"scan --path \"{environment.TemplatesDirectory}\"", ShouldSucceed = true },
            new { Command = $"scan --path \"{environment.TemplatesDirectory}\" --recursive", ShouldSucceed = true },
            new { Command = $"scan --path \"{environment.TemplatesDirectory}\" --pattern \"{{{{.*?}}}}\"", ShouldSucceed = true },
            new { Command = $"scan --path \"{environment.TemplatesDirectory}\" --format json", ShouldSucceed = true },
            new { Command = $"scan --path \"{environment.TemplatesDirectory}\" --statistics", ShouldSucceed = true },
            new { Command = $"scan --path \"{environment.TemplatesDirectory}\" --case-sensitive", ShouldSucceed = true },
            new { Command = $"scan --path \"{environment.TemplatesDirectory}\" --parallelism 2", ShouldSucceed = true },
            new { Command = $"scan --path \"{environment.TemplatesDirectory}\" --quiet", ShouldSucceed = true },

            // copy command parameters
            new { Command = $"copy --source \"{environment.TemplatesDirectory}\" --target \"{Path.Combine(environment.RootDirectory, "copy-test")}\"", ShouldSucceed = true },
            new { Command = $"copy --source \"{environment.TemplatesDirectory}\" --target \"{Path.Combine(environment.RootDirectory, "copy-test2")}\" --preserve-structure", ShouldSucceed = true },
            new { Command = $"copy --source \"{environment.TemplatesDirectory}\" --target \"{Path.Combine(environment.RootDirectory, "copy-test3")}\" --overwrite", ShouldSucceed = true },
            new { Command = $"copy --source \"{environment.TemplatesDirectory}\" --target \"{Path.Combine(environment.RootDirectory, "copy-test4")}\" --dry-run", ShouldSucceed = true },
            new { Command = $"copy --source \"{environment.TemplatesDirectory}\" --target \"{Path.Combine(environment.RootDirectory, "copy-test5")}\" --format json", ShouldSucceed = true },
            new { Command = $"copy --source \"{environment.TemplatesDirectory}\" --target \"{Path.Combine(environment.RootDirectory, "copy-test6")}\" --quiet", ShouldSucceed = true },
            new { Command = $"copy --source \"{environment.TemplatesDirectory}\" --target \"{Path.Combine(environment.RootDirectory, "copy-test7")}\" --validate", ShouldSucceed = true },

            // replace command parameters
            new { Command = $"replace --folder \"{environment.OutputDirectory}\" --map \"{mappingFile}\"", ShouldSucceed = true },
            new { Command = $"replace --folder \"{environment.OutputDirectory}\" --map \"{mappingFile}\" --backup", ShouldSucceed = true },
            new { Command = $"replace --folder \"{environment.OutputDirectory}\" --map \"{mappingFile}\" --recursive", ShouldSucceed = true },
            new { Command = $"replace --folder \"{environment.OutputDirectory}\" --map \"{mappingFile}\" --dry-run", ShouldSucceed = true },
            new { Command = $"replace --folder \"{environment.OutputDirectory}\" --map \"{mappingFile}\" --format json", ShouldSucceed = true },
            new { Command = $"replace --folder \"{environment.OutputDirectory}\" --map \"{mappingFile}\" --quiet", ShouldSucceed = true },
            new { Command = $"replace --folder \"{environment.OutputDirectory}\" --map \"{mappingFile}\" --pattern \"{{{{.*?}}}}\"", ShouldSucceed = true }
        };

        // act & assert
        foreach (var test in documentedParameterTests)
        {
            var result = await _cliExecutor.ExecuteAsync(test.Command, environment.RootDirectory);

            if (test.ShouldSucceed)
            {
                result.IsSuccess.Should().BeTrue($"Documented parameter combination should work: {test.Command}. Error: {result.StandardError}");
            }
            else
            {
                result.IsSuccess.Should().BeFalse($"Parameter combination should fail as expected: {test.Command}");
            }
        }
    }

    /// <summary>
    /// Tests that JSON output schemas match documented schemas exactly
    /// </summary>
    [Fact]
    public async Task JsonOutputSchemas_MatchDocumentation()
    {
        // arrange
        var environment = await CreateTestEnvironmentAsync("JsonSchemaValidation");
        var mappingFile = Path.Combine(environment.DataDirectory, "replacements.json");
        await File.WriteAllTextAsync(mappingFile, JsonSerializer.Serialize(new Dictionary<string, string> {
            ["{{CLIENT_NAME}}"] = "Test Corporation",
            ["{{DATE}}"] = "2025-08-18"
        }));

        // Copy templates for replace command
        await _cliExecutor.ExecuteAsync($"copy --source \"{environment.TemplatesDirectory}\" --target \"{environment.OutputDirectory}\"", environment.RootDirectory);

        var jsonSchemaTests = new[]
        {
            new {
                Command = $"list-sets --templates \"{environment.TemplatesDirectory}\" --format json",
                RequiredProperties = new[] { "command", "timestamp", "success", "data" },
                DataProperties = new[] { "template_sets", "total_sets" }
            },
            new {
                Command = $"discover --path \"{environment.TemplatesDirectory}\" --format json",
                RequiredProperties = new[] { "command", "timestamp", "success", "data" },
                DataProperties = new[] { "templates", "total_count", "total_size", "total_size_formatted" }
            },
            new {
                Command = $"scan --path \"{environment.TemplatesDirectory}\" --format json",
                RequiredProperties = new[] { "command", "timestamp", "success", "data" },
                DataProperties = new[] { "placeholders", "summary", "statistics", "errors" }
            },
            new {
                Command = $"copy --source \"{environment.TemplatesDirectory}\" --target \"{Path.Combine(environment.RootDirectory, "copy-schema-test")}\" --format json",
                RequiredProperties = new[] { "command", "timestamp", "success", "data" },
                DataProperties = new[] { "summary", "copied_files", "errors" }
            },
            new {
                Command = $"replace --folder \"{environment.OutputDirectory}\" --map \"{mappingFile}\" --format json",
                RequiredProperties = new[] { "command", "timestamp", "success", "data" },
                DataProperties = new[] { "summary", "file_results" }
            }
        };

        // act & assert
        foreach (var test in jsonSchemaTests)
        {
            var result = await _cliExecutor.ExecuteAsync(test.Command, environment.RootDirectory);

            result.IsSuccess.Should().BeTrue($"JSON command should succeed: {test.Command}. Error: {result.StandardError}");

            var jsonContent = CliProcessExecutor.ExtractJsonFromOutput(result.StandardOutput);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            // Validate top-level properties
            foreach (var prop in test.RequiredProperties)
            {
                jsonElement.TryGetProperty(prop, out _).Should().BeTrue($"JSON output should have property '{prop}' for command: {test.Command}");
            }

            // Validate data properties
            jsonElement.TryGetProperty("data", out var data).Should().BeTrue("JSON should have data property");
            foreach (var dataProp in test.DataProperties)
            {
                data.TryGetProperty(dataProp, out _).Should().BeTrue($"JSON data should have property '{dataProp}' for command: {test.Command}");
            }

            // Validate command field matches expected command name
            var commandName = jsonElement.GetProperty("command").GetString();
            test.Command.Should().StartWith(commandName!, $"Command field should match actual command name for: {test.Command}");

            // Validate success field is boolean
            jsonElement.GetProperty("success").ValueKind.Should().Be(JsonValueKind.True, "Success field should be boolean true for successful commands");
        }
    }

    /// <summary>
    /// Tests help output for all commands contains documented parameters
    /// </summary>
    [Fact]
    public async Task HelpOutput_ContainsDocumentedParameters()
    {
        var helpTests = new[]
        {
            new { Command = "list-sets --help", RequiredText = new[] { "--templates", "--format", "--details", "--include-empty" } },
            new { Command = "discover --help", RequiredText = new[] { "--path", "--recursive", "--format", "--include", "--exclude", "--max-depth", "--min-size", "--quiet" } },
            new { Command = "scan --help", RequiredText = new[] { "--path", "--recursive", "--pattern", "--format", "--statistics", "--case-sensitive", "--parallelism", "--quiet" } },
            new { Command = "copy --help", RequiredText = new[] { "--source", "--target", "--preserve-structure", "--overwrite", "--dry-run", "--format", "--quiet", "--validate" } },
            new { Command = "replace --help", RequiredText = new[] { "--folder", "--map", "--backup", "--recursive", "--dry-run", "--format", "--quiet", "--pattern" } }
        };

        foreach (var test in helpTests)
        {
            var result = await _cliExecutor.ExecuteAsync(test.Command, Environment.CurrentDirectory);

            result.IsSuccess.Should().BeTrue($"Help command should succeed: {test.Command}");

            foreach (var requiredText in test.RequiredText)
            {
                result.StandardOutput.Should().Contain(requiredText, $"Help output for '{test.Command}' should document parameter '{requiredText}'");
            }
        }
    }

    /// <summary>
    /// Tests error scenarios produce appropriate error messages and exit codes
    /// </summary>
    [Fact]
    public async Task ErrorScenarios_ProduceCorrectErrorMessages()
    {
        var errorTests = new[]
        {
            // Missing required parameters
            new { Command = "list-sets", ExpectedExitCode = 1, ShouldContainError = "templates" },
            new { Command = "discover", ExpectedExitCode = 1, ShouldContainError = "path" },
            new { Command = "scan", ExpectedExitCode = 1, ShouldContainError = "path" },
            new { Command = "copy", ExpectedExitCode = 1, ShouldContainError = "source" },
            new { Command = "replace", ExpectedExitCode = 1, ShouldContainError = "folder" },

            // Invalid paths
            new { Command = "list-sets --templates \"/nonexistent/path\"", ExpectedExitCode = 1, ShouldContainError = "not found" },
            new { Command = "discover --path \"/nonexistent/path\"", ExpectedExitCode = 1, ShouldContainError = "not found" },
            new { Command = "copy --source \"/nonexistent/path\" --target \"/tmp\"", ExpectedExitCode = 1, ShouldContainError = "not found" },
            new { Command = "replace --folder \"/nonexistent/path\" --map \"/nonexistent.json\"", ExpectedExitCode = 1, ShouldContainError = "not found" }
        };

        foreach (var test in errorTests)
        {
            var result = await _cliExecutor.ExecuteAsync(test.Command, Environment.CurrentDirectory);

            result.IsSuccess.Should().BeFalse($"Error command should fail: {test.Command}");
            result.ExitCode.Should().Be(test.ExpectedExitCode, $"Exit code should match documented exit code for: {test.Command}");

            var errorOutput = result.StandardError + " " + result.StandardOutput;
            errorOutput.ToLower().Should().Contain(test.ShouldContainError.ToLower(),
                $"Error output should contain '{test.ShouldContainError}' for command: {test.Command}. Actual output: {errorOutput}");
        }
    }

    /// <summary>
    /// Tests output format options (text, json, csv, table) work as documented
    /// </summary>
    [Fact]
    public async Task OutputFormats_WorkAsDocumented()
    {
        // arrange
        var environment = await CreateTestEnvironmentAsync("OutputFormats");

        var formatTests = new[]
        {
            new { Command = $"list-sets --templates \"{environment.TemplatesDirectory}\" --format text", ExpectedFormat = "text" },
            new { Command = $"list-sets --templates \"{environment.TemplatesDirectory}\" --format json", ExpectedFormat = "json" },
            new { Command = $"discover --path \"{environment.TemplatesDirectory}\" --format text", ExpectedFormat = "text" },
            new { Command = $"discover --path \"{environment.TemplatesDirectory}\" --format json", ExpectedFormat = "json" },
            new { Command = $"discover --path \"{environment.TemplatesDirectory}\" --format csv", ExpectedFormat = "csv" },
            new { Command = $"discover --path \"{environment.TemplatesDirectory}\" --format table", ExpectedFormat = "table" },
            new { Command = $"scan --path \"{environment.TemplatesDirectory}\" --format text", ExpectedFormat = "text" },
            new { Command = $"scan --path \"{environment.TemplatesDirectory}\" --format json", ExpectedFormat = "json" },
            new { Command = $"scan --path \"{environment.TemplatesDirectory}\" --format csv", ExpectedFormat = "csv" },
            new { Command = $"scan --path \"{environment.TemplatesDirectory}\" --format table", ExpectedFormat = "table" }
        };

        foreach (var test in formatTests)
        {
            var result = await _cliExecutor.ExecuteAsync(test.Command, environment.RootDirectory);

            result.IsSuccess.Should().BeTrue($"Format command should succeed: {test.Command}. Error: {result.StandardError}");

            if (test.ExpectedFormat == "json")
            {
                var jsonContent = CliProcessExecutor.ExtractJsonFromOutput(result.StandardOutput);
                var jsonValidation = () => JsonSerializer.Deserialize<JsonElement>(jsonContent);
                jsonValidation.Should().NotThrow($"JSON format should produce valid JSON for: {test.Command}");
            }
            else if (test.ExpectedFormat == "csv")
            {
                result.StandardOutput.Should().Contain(",", $"CSV format should contain commas for: {test.Command}");
            }
            else if (test.ExpectedFormat == "table")
            {
                // Table format typically uses Unicode box-drawing characters or pipes
                var hasTableChars = result.StandardOutput.Contains("|") || result.StandardOutput.Contains("─") || result.StandardOutput.Contains("│");
                hasTableChars.Should().BeTrue($"Table format should contain table formatting characters for: {test.Command}");
            }
        }
    }

    /// <summary>
    /// Tests that CLI version and help match documented structure
    /// </summary>
    [Fact]
    public async Task VersionAndHelp_MatchDocumentedStructure()
    {
        // Test version output
        var versionResult = await _cliExecutor.ExecuteAsync("--version", Environment.CurrentDirectory);
        versionResult.IsSuccess.Should().BeTrue("Version command should succeed");
        versionResult.StandardOutput.Should().NotBeNullOrWhiteSpace("Version should produce output");

        // Test main help output
        var helpResult = await _cliExecutor.ExecuteAsync("--help", Environment.CurrentDirectory);
        helpResult.IsSuccess.Should().BeTrue("Help command should succeed");
        helpResult.StandardOutput.Should().Contain("DocxTemplate CLI", "Help should contain application name");
        helpResult.StandardOutput.Should().Contain("list-sets", "Help should list list-sets command");
        helpResult.StandardOutput.Should().Contain("discover", "Help should list discover command");
        helpResult.StandardOutput.Should().Contain("scan", "Help should list scan command");
        helpResult.StandardOutput.Should().Contain("copy", "Help should list copy command");
        helpResult.StandardOutput.Should().Contain("replace", "Help should list replace command");
    }

    private async Task<TestEnvironment> CreateTestEnvironmentAsync(string testName)
    {
        var spec = new TestEnvironmentSpec
        {
            Name = testName,
            TemplateSets =
            [
                new()
                {
                    Name = "TestTemplateSet",
                    DocumentCount = 3,
                    Placeholders = ["CLIENT_NAME", "DATE", "AMOUNT"],
                    IncludeCzechCharacters = false
                }
            ],
            ReplacementMappings =
            [
                new()
                {
                    Name = "standard",
                    Values = new Dictionary<string, string>
                    {
                        { "CLIENT_NAME", "Test Corporation" },
                        { "DATE", "2025-08-18" },
                        { "AMOUNT", "$10,000.00" }
                    }
                }
            ]
        };

        var environment = await _environmentProvisioner.CreateTestEnvironmentAsync(spec);
        _testEnvironments.Add(environment);
        return environment;
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
