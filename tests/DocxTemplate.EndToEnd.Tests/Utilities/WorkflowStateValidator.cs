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
                    var jsonOutput = JsonSerializer.Deserialize<JsonElement>(commandResult.StandardOutput);
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
        switch (commandName.ToLower())
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
        if (!jsonOutput.TryGetProperty("templateSets", out var templateSets))
        {
            result.ValidationErrors.Add("list-sets output missing 'templateSets' property");
            result.IsValid = false;
            return;
        }

        if (templateSets.ValueKind != JsonValueKind.Array)
        {
            result.ValidationErrors.Add("list-sets 'templateSets' property should be an array");
            result.IsValid = false;
        }
        
        await Task.CompletedTask;
    }

    private async Task ValidateDiscoverOutput(JsonElement jsonOutput, WorkflowValidationResult result)
    {
        if (!jsonOutput.TryGetProperty("templateSet", out _))
        {
            result.ValidationErrors.Add("discover output missing 'templateSet' property");
            result.IsValid = false;
        }

        if (!jsonOutput.TryGetProperty("templates", out var templates))
        {
            result.ValidationErrors.Add("discover output missing 'templates' property");
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
        if (!jsonOutput.TryGetProperty("placeholders", out var placeholders))
        {
            result.ValidationErrors.Add("scan output missing 'placeholders' property");
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
        if (!jsonOutput.TryGetProperty("copiedFiles", out var copiedFiles))
        {
            result.ValidationErrors.Add("copy output missing 'copiedFiles' property");
            result.IsValid = false;
            return;
        }

        if (copiedFiles.ValueKind != JsonValueKind.Array)
        {
            result.ValidationErrors.Add("copy 'copiedFiles' property should be an array");
            result.IsValid = false;
        }
        
        await Task.CompletedTask;
    }

    private async Task ValidateReplaceOutput(JsonElement jsonOutput, WorkflowValidationResult result)
    {
        if (!jsonOutput.TryGetProperty("replacements", out var replacements))
        {
            result.ValidationErrors.Add("replace output missing 'replacements' property");
            result.IsValid = false;
            return;
        }

        if (replacements.ValueKind != JsonValueKind.Array)
        {
            result.ValidationErrors.Add("replace 'replacements' property should be an array");
            result.IsValid = false;
        }
        
        await Task.CompletedTask;
    }

    private async Task ValidateDataConsistencyAcrossCommands(
        Dictionary<string, JsonElement> commandOutputs, 
        WorkflowExpectation expectations,
        WorkflowValidationResult result)
    {
        // Validate template set consistency between discover and other commands
        if (commandOutputs.ContainsKey("discover") && commandOutputs.ContainsKey("scan"))
        {
            var discoverSetName = commandOutputs["discover"].GetProperty("templateSet").GetProperty("name").GetString();
            
            if (commandOutputs["scan"].TryGetProperty("templateSet", out var scanTemplateSet))
            {
                var scanSetName = scanTemplateSet.GetProperty("name").GetString();
                if (discoverSetName != scanSetName)
                {
                    result.ValidationErrors.Add($"Template set inconsistency between discover ('{discoverSetName}') and scan ('{scanSetName}')");
                    result.IsValid = false;
                }
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
    public List<string> ExpectedCommandSequence { get; set; } = new();
    public List<int> ExpectedFailures { get; set; } = new();
    public List<string> RequireJsonOutput { get; set; } = new();
    public bool AllowTemplateSetChanges { get; set; } = false;
    public bool RequireTemplateSetConsistency { get; set; } = true;
}

/// <summary>
/// Result of workflow validation
/// </summary>
public class WorkflowValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<CliExecutionResult> CommandResults { get; set; } = new();
}