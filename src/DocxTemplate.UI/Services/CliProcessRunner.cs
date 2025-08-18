using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DocxTemplate.UI.Services;

public class CliProcessRunner : ICliCommandService
{
    private readonly string _cliExecutablePath;
    private readonly int _timeoutMs;

    public CliProcessRunner()
    {
        // Default to looking for the CLI executable in the same directory or PATH
        _cliExecutablePath = "docx-template";
        _timeoutMs = 30000; // 30 second default timeout
    }

    public CliProcessRunner(string cliExecutablePath, int timeoutMs = 30000)
    {
        _cliExecutablePath = cliExecutablePath ?? throw new ArgumentNullException(nameof(cliExecutablePath));
        _timeoutMs = timeoutMs;
    }

    public async Task<string> ExecuteCommandAsync(string command, string[] arguments)
    {
        var fullCommand = string.Join(" ", new[] { command }.Concat(arguments ?? Array.Empty<string>()));
        
        var processStartInfo = new ProcessStartInfo
        {
            FileName = _cliExecutablePath,
            Arguments = fullCommand,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        
        try
        {
            process.Start();
            
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            // Wait for process completion with timeout
            var completed = await WaitForExitAsync(process, TimeSpan.FromMilliseconds(_timeoutMs));
            
            if (!completed)
            {
                process.Kill();
                throw new TimeoutException($"CLI command '{command}' timed out after {_timeoutMs}ms");
            }
            
            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"CLI command '{command}' failed with exit code {process.ExitCode}: {error}");
            }

            return output;
        }
        catch (Exception ex) when (!(ex is TimeoutException || ex is InvalidOperationException))
        {
            throw new InvalidOperationException($"Failed to execute CLI command '{command}': {ex.Message}", ex);
        }
    }

    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => tcs.TrySetResult(true);
        
        if (process.HasExited)
        {
            return true;
        }

        var timeoutTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
        
        return completedTask == tcs.Task;
    }
}