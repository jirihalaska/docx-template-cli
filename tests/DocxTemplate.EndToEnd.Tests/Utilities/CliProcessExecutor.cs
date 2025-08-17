using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace DocxTemplate.EndToEnd.Tests.Utilities;

/// <summary>
/// Executes real CLI processes for end-to-end testing
/// </summary>
public class CliProcessExecutor : IDisposable
{
    private readonly string _cliExecutablePath;
    private readonly TimeSpan _defaultTimeout;
    private bool _disposed;

    public CliProcessExecutor(string cliExecutablePath, TimeSpan? defaultTimeout = null)
    {
        _cliExecutablePath = cliExecutablePath ?? throw new ArgumentNullException(nameof(cliExecutablePath));
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromMinutes(2);
        
        if (!File.Exists(_cliExecutablePath))
        {
            throw new FileNotFoundException($"CLI executable not found at: {_cliExecutablePath}");
        }
    }

    /// <summary>
    /// Executes a CLI command and returns the result
    /// </summary>
    public async Task<CliExecutionResult> ExecuteAsync(
        string command, 
        string? workingDirectory = null, 
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CliProcessExecutor));

        var actualTimeout = timeout ?? _defaultTimeout;
        var startInfo = new ProcessStartInfo
        {
            FileName = _cliExecutablePath,
            Arguments = command,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);
            var timedOut = stopwatch.Elapsed > actualTimeout;

            if (timedOut)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception killEx)
                {
                    // Log kill exception but don't throw
                    System.Diagnostics.Debug.WriteLine($"Failed to kill process: {killEx.Message}");
                }
                
                throw new TimeoutException($"CLI command '{command}' timed out after {actualTimeout}");
            }

            return new CliExecutionResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = outputBuilder.ToString().Trim(),
                StandardError = errorBuilder.ToString().Trim(),
                Duration = stopwatch.Elapsed,
                Command = command,
                WorkingDirectory = workingDirectory ?? string.Empty
            };
        }
        finally
        {
            stopwatch.Stop();
            process?.Dispose();
        }
    }

    /// <summary>
    /// Executes a CLI command and deserializes JSON output
    /// </summary>
    public async Task<T> ExecuteAndDeserializeAsync<T>(
        string command, 
        string? workingDirectory = null, 
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync(command, workingDirectory, timeout, cancellationToken);
        
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"CLI command failed with exit code {result.ExitCode}. Error: {result.StandardError}");
        }

        if (string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            throw new InvalidOperationException("CLI command returned empty output");
        }

        var jsonContent = ExtractJsonFromOutput(result.StandardOutput);

        try
        {
            return JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize CLI output as {typeof(T).Name}. JSON: {jsonContent}", ex);
        }
    }

    /// <summary>
    /// Extracts JSON content from mixed CLI output
    /// </summary>
    public static string ExtractJsonFromOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return output;

        // Look for JSON starting with { or [
        var lines = output.Split('\n');
        var jsonStart = -1;
        
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
            {
                jsonStart = i;
                break;
            }
        }

        if (jsonStart == -1)
        {
            // No JSON found, return original output
            return output;
        }

        // Extract from the first JSON line to the end
        var jsonLines = lines.Skip(jsonStart);
        return string.Join('\n', jsonLines).Trim();
    }

    /// <summary>
    /// Builds the CLI executable path for testing
    /// </summary>
    public static string GetCliExecutablePath(string configuration = "Debug")
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(CliProcessExecutor).Assembly.Location) ?? throw new InvalidOperationException("Unable to determine assembly location");
        var solutionRoot = FindSolutionRoot(assemblyDirectory);
        
        var cliProjectPath = Path.Combine(solutionRoot, "src", "DocxTemplate.CLI", "bin", configuration, "net9.0");
        
        // Try different executable names based on platform
        var possibleNames = new[]
        {
            "DocxTemplate.CLI.exe",  // Windows
            "DocxTemplate.CLI",      // Unix
            "docx-template.exe",     // Potential published name
            "docx-template"          // Potential published name Unix
        };

        foreach (var name in possibleNames)
        {
            var path = Path.Combine(cliProjectPath, name);
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException($"CLI executable not found in {cliProjectPath}. Available names tried: {string.Join(", ", possibleNames)}");
    }

    private static string FindSolutionRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        
        while (current != null && !current.GetFiles("*.sln").Any())
        {
            current = current.Parent;
        }

        if (current == null)
            throw new DirectoryNotFoundException("Could not find solution root directory");

        return current.FullName;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

/// <summary>
/// Result of CLI command execution
/// </summary>
public class CliExecutionResult
{
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string Command { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;

    public bool IsSuccess => ExitCode == 0;
    public bool HasOutput => !string.IsNullOrWhiteSpace(StandardOutput);
    public bool HasError => !string.IsNullOrWhiteSpace(StandardError);
}