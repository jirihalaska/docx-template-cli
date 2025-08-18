using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DocxTemplate.UI.Services;

public class CliProcessRunner : ICliCommandService
{
    private readonly string _cliExecutablePath;

    public CliProcessRunner()
    {
        // Default to looking for the CLI executable in the same directory or PATH
        _cliExecutablePath = "docx-template";
    }

    public CliProcessRunner(string cliExecutablePath)
    {
        _cliExecutablePath = cliExecutablePath ?? throw new ArgumentNullException(nameof(cliExecutablePath));
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
        
        process.Start();
        
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();
        
        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"CLI command failed with exit code {process.ExitCode}: {error}");
        }

        return output;
    }
}