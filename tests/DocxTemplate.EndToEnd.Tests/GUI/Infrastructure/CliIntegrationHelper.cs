using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;

namespace DocxTemplate.EndToEnd.Tests.GUI.Infrastructure;

public class CliIntegrationHelper
{
    private readonly string _cliExecutablePath;

    public CliIntegrationHelper()
    {
        // Find CLI executable by navigating up from test assembly location to solution root
        var testAssemblyLocation = Path.GetDirectoryName(typeof(CliIntegrationHelper).Assembly.Location)!;
        var solutionRoot = testAssemblyLocation;
        
        // Navigate up from test bin directory to solution root
        while (!Directory.Exists(Path.Combine(solutionRoot, "src")) && Directory.GetParent(solutionRoot) != null)
        {
            solutionRoot = Directory.GetParent(solutionRoot)!.FullName;
        }
        
        var cliPath = Path.Combine(solutionRoot, "src", "DocxTemplate.CLI", "bin", "Debug", "net9.0", "DocxTemplate.CLI.dll");
        
        if (!File.Exists(cliPath))
        {
            throw new InvalidOperationException($"CLI executable not found at: {cliPath}. Build the CLI project first. Test assembly location: {testAssemblyLocation}, Solution root: {solutionRoot}");
        }
        
        _cliExecutablePath = cliPath;
    }

    public async Task<List<string>> GetTemplateSetsCli(string templatesPath)
    {
        var result = await ExecuteCliCommandAsync("list-sets", $"--templates \"{templatesPath}\" --format json");
        
        // Parse JSON result to extract template set names - CLI returns {data: {template_sets: [...]}}
        using var jsonDoc = JsonDocument.Parse(result);
        var templateSets = new List<string>();
        
        if (jsonDoc.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty("template_sets", out var sets))
        {
            foreach (var set in sets.EnumerateArray())
            {
                if (set.TryGetProperty("name", out var name))
                {
                    templateSets.Add(name.GetString()!);
                }
            }
        }

        return templateSets;
    }

    public async Task<Dictionary<string, string[]>> ScanPlaceholdersCli(string templatePath)
    {
        var result = await ExecuteCliCommandAsync("scan", $"--path \"{templatePath}\" --format json");
        
        // Parse JSON result to extract placeholders - CLI returns {data: {placeholders: [...]}}
        using var jsonDoc = JsonDocument.Parse(result);
        var placeholders = new Dictionary<string, string[]>();
        
        if (jsonDoc.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty("placeholders", out var phElement))
        {
            foreach (var placeholder in phElement.EnumerateArray())
            {
                if (placeholder.TryGetProperty("name", out var name) &&
                    placeholder.TryGetProperty("locations", out var locations))
                {
                    var locationsList = new List<string>();
                    foreach (var location in locations.EnumerateArray())
                    {
                        if (location.TryGetProperty("file_name", out var fileName))
                        {
                            locationsList.Add(fileName.GetString()!);
                        }
                    }
                    placeholders[name.GetString()!] = locationsList.ToArray();
                }
            }
        }

        return placeholders;
    }

    public async Task<bool> CopyTemplatesCli(string source, string target)
    {
        var result = await ExecuteCliCommandAsync("copy", $"--source \"{source}\" --target \"{target}\" --format json");
        
        using var jsonDoc = JsonDocument.Parse(result);
        return jsonDoc.RootElement.TryGetProperty("success", out var success) && success.GetBoolean();
    }

    public async Task<bool> ReplacePlaceholdersCli(string folder, string mapFile)
    {
        var result = await ExecuteCliCommandAsync("replace", $"--folder \"{folder}\" --map \"{mapFile}\" --format json");
        
        using var jsonDoc = JsonDocument.Parse(result);
        return jsonDoc.RootElement.TryGetProperty("success", out var success) && success.GetBoolean();
    }

    public async Task<CliWorkflowResult> ExecuteFullWorkflowCli(string templatesPath, string templateSetName, Dictionary<string, string> placeholderValues, string outputPath)
    {
        var result = new CliWorkflowResult();
        
        try
        {
            // Step 1: List template sets
            result.TemplateSets = await GetTemplateSetsCli(templatesPath);
            result.TemplateSets.Should().Contain(templateSetName, "Template set should be found by CLI");

            // Step 2: Scan placeholders
            var templateSetPath = Path.Combine(templatesPath, templateSetName);
            result.Placeholders = await ScanPlaceholdersCli(templateSetPath);
            result.Placeholders.Should().NotBeEmpty("CLI should find placeholders in templates");

            // Step 3: Copy templates
            result.CopySuccess = await CopyTemplatesCli(templateSetPath, outputPath);
            result.CopySuccess.Should().BeTrue("CLI should successfully copy templates");

            // Step 4: Create placeholder values file
            var valuesFile = Path.Combine(outputPath, "placeholder_values.json");
            var jsonContent = JsonSerializer.Serialize(placeholderValues, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(valuesFile, jsonContent);

            // Step 5: Replace placeholders
            result.ReplaceSuccess = await ReplacePlaceholdersCli(outputPath, valuesFile);
            result.ReplaceSuccess.Should().BeTrue("CLI should successfully replace placeholders");

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    private async Task<string> ExecuteCliCommandAsync(string command, string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{_cliExecutablePath}\" {command} {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"CLI command '{command}' failed with exit code {process.ExitCode}: {error}. Output: {output}");
        }

        // Extract JSON from CLI output (may contain progress messages before JSON)
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string? jsonOutput = null;
        
        // Find the line that starts with JSON (either { or [)
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("{") || trimmedLine.StartsWith("["))
            {
                // Found JSON start, take this line and all remaining lines
                var jsonStartIndex = Array.IndexOf(lines, line);
                jsonOutput = string.Join('\n', lines.Skip(jsonStartIndex));
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(jsonOutput))
        {
            throw new InvalidOperationException($"CLI command '{command}' did not return valid JSON. Output: {output}");
        }

        return jsonOutput.Trim();
    }
}

public class CliWorkflowResult
{
    public List<string> TemplateSets { get; set; } = new();
    public Dictionary<string, string[]> Placeholders { get; set; } = new();
    public bool CopySuccess { get; set; }
    public bool ReplaceSuccess { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}