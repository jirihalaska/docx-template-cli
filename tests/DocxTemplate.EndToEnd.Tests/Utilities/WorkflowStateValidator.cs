using System.Globalization;
using System.Text.Json;

namespace DocxTemplate.EndToEnd.Tests.Utilities;

/// <summary>
/// Validates workflow state and transitions between CLI commands
/// </summary>
public class WorkflowStateValidator
{
    /// <summary>
    /// Validates that workflow state is maintained correctly across command executions
    /// </summary>
    public async Task<WorkflowValidationResult> ValidateWorkflowStateAsync(
        List<CliExecutionResult> commandResults,
        WorkflowExpectation expectations)
    {
        var result = new WorkflowValidationResult
        {
            IsValid = true,
            CommandResults = commandResults
        };

        try
        {
            // Validate command sequence
            await ValidateCommandSequenceAsync(commandResults, expectations, result);

            // Validate data flow between commands
            await ValidateDataFlowAsync(commandResults, expectations, result);

            // Validate state consistency
            await ValidateStateConsistencyAsync(commandResults, expectations, result);

            // Validate error handling
            await ValidateErrorHandlingAsync(commandResults, expectations, result);
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ValidationErrors.Add($"Workflow validation failed: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Validates that commands execute in the correct sequence
    /// </summary>
    private async Task ValidateCommandSequenceAsync(
        List<CliExecutionResult> commandResults,
        WorkflowExpectation expectations,
        WorkflowValidationResult result)
    {
        if (commandResults.Count != expectations.ExpectedCommandSequence.Count)
        {
            result.ValidationErrors.Add($"Command count mismatch: expected {expectations.ExpectedCommandSequence.Count}, got {commandResults.Count}");
            result.IsValid = false;
            return;
        }

        for (int i = 0; i < commandResults.Count; i++)
        {
            var actualCommand = ExtractCommandName(commandResults[i].Command);
            var expectedCommand = expectations.ExpectedCommandSequence[i];

            if (actualCommand != expectedCommand)
            {
                result.ValidationErrors.Add($"Command sequence error at position {i}: expected '{expectedCommand}', got '{actualCommand}'");
                result.IsValid = false;
            }

            // Validate command success
            if (!commandResults[i].IsSuccess && !expectations.ExpectedFailures.Contains(i))
            {
                result.ValidationErrors.Add($"Unexpected command failure at position {i}: {commandResults[i].StandardError}");
                result.IsValid = false;
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates data flow and JSON output consistency between commands
    /// </summary>
    private async Task ValidateDataFlowAsync(
        List<CliExecutionResult> commandResults,
        WorkflowExpectation expectations,
        WorkflowValidationResult result)
    {
        var commandOutputs = new Dictionary<string, JsonElement>();

        foreach (var commandResult in commandResults)
        {
            if (commandResult.IsSuccess && commandResult.HasOutput)
            {
                try
                {
                    var commandName = ExtractCommandName(commandResult.Command);
                    var jsonContent = CliProcessExecutor.ExtractJsonFromOutput(commandResult.StandardOutput);
                    var jsonOutput = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                    commandOutputs[commandName] = jsonOutput;

                    // Validate JSON schema based on command type
                    await ValidateCommandJsonSchema(commandName, jsonOutput, result);
                }
                catch (JsonException ex)
                {
                    if (expectations.RequireJsonOutput.Contains(ExtractCommandName(commandResult.Command)))
                    {
                        result.ValidationErrors.Add($"Invalid JSON output from {commandResult.Command}: {ex.Message}");
                        result.IsValid = false;
                    }
                }
            }
        }

        // Validate data consistency across commands
        await ValidateDataConsistencyAcrossCommands(commandOutputs, expectations, result);
    }

    /// <summary>
    /// Validates state consistency throughout the workflow
    /// </summary>
    private async Task ValidateStateConsistencyAsync(
        List<CliExecutionResult> commandResults,
        WorkflowExpectation expectations,
        WorkflowValidationResult result)
    {
        // Track template set context
        string? currentTemplateSet = null;
        string? currentTemplatesPath = null;
        string? currentTargetPath = null;

        foreach (var commandResult in commandResults)
        {
            var command = commandResult.Command;

            // Extract template set from command if present
            if (command.Contains("--set"))
            {
                var setMatch = System.Text.RegularExpressions.Regex.Match(command, @"--set\s+""?([^""]*?)""?(?:\s|$)");
                if (setMatch.Success)
                {
                    var newTemplateSet = setMatch.Groups[1].Value;
                    if (currentTemplateSet != null && currentTemplateSet != newTemplateSet)
                    {
                        if (!expectations.AllowTemplateSetChanges)
                        {
                            result.ValidationErrors.Add($"Unexpected template set change from '{currentTemplateSet}' to '{newTemplateSet}'");
                            result.IsValid = false;
                        }
                    }
                    currentTemplateSet = newTemplateSet;
                }
            }

            // Extract templates path
            if (command.Contains("--templates"))
            {
                var templatesMatch = System.Text.RegularExpressions.Regex.Match(command, @"--templates\s+""?([^""]*?)""?(?:\s|$)");
                if (templatesMatch.Success)
                {
                    currentTemplatesPath = templatesMatch.Groups[1].Value;
                }
            }

            // Extract target path
            if (command.Contains("--target"))
            {
                var targetMatch = System.Text.RegularExpressions.Regex.Match(command, @"--target\s+""?([^""]*?)""?(?:\s|$)");
                if (targetMatch.Success)
                {
                    currentTargetPath = targetMatch.Groups[1].Value;
                }
            }
        }

        // Validate final state
        if (expectations.RequireTemplateSetConsistency && string.IsNullOrEmpty(currentTemplateSet))
        {
            result.ValidationErrors.Add("Template set context not maintained throughout workflow");
            result.IsValid = false;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates error handling and recovery mechanisms
    /// </summary>
    private async Task ValidateErrorHandlingAsync(
        List<CliExecutionResult> commandResults,
        WorkflowExpectation expectations,
        WorkflowValidationResult result)
    {
        for (int i = 0; i < commandResults.Count; i++)
        {
            var commandResult = commandResults[i];

            if (!commandResult.IsSuccess)
            {
                // Check if this failure was expected
                if (!expectations.ExpectedFailures.Contains(i))
                {
                    result.ValidationErrors.Add($"Unexpected command failure at position {i}: {commandResult.StandardError}");
                    result.IsValid = false;
                }
                else
                {
                    // Validate error message quality
                    if (string.IsNullOrWhiteSpace(commandResult.StandardError))
                    {
                        result.ValidationErrors.Add($"Expected command failure at position {i} did not provide error message");
                        result.IsValid = false;
                    }
                }
            }
        }

        await Task.CompletedTask;
    }

    private async Task ValidateCommandJsonSchema(string commandName, JsonElement jsonOutput, WorkflowValidationResult result)
    {
        switch (commandName.ToLower(CultureInfo.InvariantCulture))
        {
            case "list-sets":
                await ValidateListSetsOutput(jsonOutput, result);
                break;
            case "discover":
                await ValidateDiscoverOutput(jsonOutput, result);
                break;
            case "scan":
                await ValidateScanOutput(jsonOutput, result);
                break;
            case "copy":
                await ValidateCopyOutput(jsonOutput, result);
                break;
            case "replace":
                await ValidateReplaceOutput(jsonOutput, result);
                break;
        }

        await Task.CompletedTask;
    }

    private async Task ValidateListSetsOutput(JsonElement jsonOutput, WorkflowValidationResult result)
    {
        if (!jsonOutput.TryGetProperty("data", out var data))
        {
            result.ValidationErrors.Add("list-sets output missing 'data' property");
            result.IsValid = false;
            return;
        }

        if (!data.TryGetProperty("template_sets", out var templateSets))
        {
            result.ValidationErrors.Add("list-sets output missing 'template_sets' property in data");
            result.IsValid = false;
            return;
        }

        if (templateSets.ValueKind != JsonValueKind.Array)
        {
            result.ValidationErrors.Add("list-sets 'template_sets' property should be an array");
            result.IsValid = false;
        }

        await Task.CompletedTask;
    }

    private async Task ValidateDiscoverOutput(JsonElement jsonOutput, WorkflowValidationResult result)
    {
        if (!jsonOutput.TryGetProperty("data", out var data))
        {
            result.ValidationErrors.Add("discover output missing 'data' property");
            result.IsValid = false;
            return;
        }

        if (!data.TryGetProperty("templates", out var templates))
        {
            result.ValidationErrors.Add("discover output missing 'templates' property in data");
            result.IsValid = false;
            return;
        }

        if (templates.ValueKind != JsonValueKind.Array)
        {
            result.ValidationErrors.Add("discover 'templates' property should be an array");
            result.IsValid = false;
        }

        await Task.CompletedTask;
    }

    private async Task ValidateScanOutput(JsonElement jsonOutput, WorkflowValidationResult result)
    {
        if (!jsonOutput.TryGetProperty("data", out var data))
        {
            result.ValidationErrors.Add("scan output missing 'data' property");
            result.IsValid = false;
            return;
        }

        if (!data.TryGetProperty("placeholders", out var placeholders))
        {
            result.ValidationErrors.Add("scan output missing 'placeholders' property in data");
            result.IsValid = false;
            return;
        }

        if (placeholders.ValueKind != JsonValueKind.Array)
        {
            result.ValidationErrors.Add("scan 'placeholders' property should be an array");
            result.IsValid = false;
        }

        await Task.CompletedTask;
    }

    private async Task ValidateCopyOutput(JsonElement jsonOutput, WorkflowValidationResult result)
    {
        if (!jsonOutput.TryGetProperty("data", out var data))
        {
            result.ValidationErrors.Add("copy output missing 'data' property");
            result.IsValid = false;
            return;
        }

        if (!data.TryGetProperty("copied_files", out var copiedFiles))
        {
            result.ValidationErrors.Add("copy output missing 'copied_files' property in data");
            result.IsValid = false;
            return;
        }

        if (copiedFiles.ValueKind != JsonValueKind.Array)
        {
            result.ValidationErrors.Add("copy 'copied_files' property should be an array");
            result.IsValid = false;
        }

        await Task.CompletedTask;
    }

    private async Task ValidateReplaceOutput(JsonElement jsonOutput, WorkflowValidationResult result)
    {
        if (!jsonOutput.TryGetProperty("data", out var data))
        {
            result.ValidationErrors.Add("replace output missing 'data' property");
            result.IsValid = false;
            return;
        }

        if (!data.TryGetProperty("summary", out var summary))
        {
            result.ValidationErrors.Add("replace output missing 'summary' property in data");
            result.IsValid = false;
            return;
        }

        if (!data.TryGetProperty("file_results", out var fileResults))
        {
            result.ValidationErrors.Add("replace output missing 'file_results' property in data");
            result.IsValid = false;
            return;
        }

        if (fileResults.ValueKind != JsonValueKind.Array)
        {
            result.ValidationErrors.Add("replace 'file_results' property should be an array");
            result.IsValid = false;
        }

        await Task.CompletedTask;
    }

    private async Task ValidateDataConsistencyAcrossCommands(
        Dictionary<string, JsonElement> commandOutputs,
        WorkflowExpectation expectations,
        WorkflowValidationResult result)
    {
        // Since we're dealing with directory-based operations rather than template sets,
        // we'll validate that the commands are operating on consistent data structures
        foreach (var (commandName, output) in commandOutputs)
        {
            // Validate that all JSON outputs have the expected basic structure
            if (!output.TryGetProperty("command", out _))
            {
                result.ValidationErrors.Add($"Command '{commandName}' output missing 'command' property");
                result.IsValid = false;
            }

            if (!output.TryGetProperty("success", out _))
            {
                result.ValidationErrors.Add($"Command '{commandName}' output missing 'success' property");
                result.IsValid = false;
            }

            if (!output.TryGetProperty("data", out _))
            {
                result.ValidationErrors.Add($"Command '{commandName}' output missing 'data' property");
                result.IsValid = false;
            }
        }

        await Task.CompletedTask;
    }

    private string ExtractCommandName(string fullCommand)
    {
        var parts = fullCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : string.Empty;
    }
}

/// <summary>
/// Expectations for workflow validation
/// </summary>
public class WorkflowExpectation
{
    public List<string> ExpectedCommandSequence { get; set; } = [];
    public List<int> ExpectedFailures { get; set; } = [];
    public List<string> RequireJsonOutput { get; set; } = [];
    public bool AllowTemplateSetChanges { get; set; } = false;
    public bool RequireTemplateSetConsistency { get; init; } = true;
}

/// <summary>
/// Result of workflow validation
/// </summary>
public class WorkflowValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = [];
    public List<CliExecutionResult> CommandResults { get; set; } = [];
}
