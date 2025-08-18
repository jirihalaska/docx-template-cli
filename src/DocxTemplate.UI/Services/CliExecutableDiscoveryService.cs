using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DocxTemplate.UI.Services;

public class CliExecutableDiscoveryService : ICliExecutableDiscoveryService
{
    private readonly string[] _executableNames;

    public CliExecutableDiscoveryService()
    {
        // Prefer DLL over executable since the executables may not have proper runtime
        // Platform-specific executable names - DLL first, then executables
        _executableNames = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? new[] { "DocxTemplate.CLI.dll", "docx-template.exe", "DocxTemplate.CLI.exe" }
            : new[] { "DocxTemplate.CLI.dll", "docx-template", "DocxTemplate.CLI" };
    }

    public Task<string> DiscoverCliExecutableAsync()
    {
        var guiExecutableDirectory = GetGuiExecutableDirectory();
        
        foreach (var executableName in _executableNames)
        {
            var candidatePath = Path.Combine(guiExecutableDirectory, executableName);
            
            if (File.Exists(candidatePath))
            {
                // Skip validation during startup to avoid deadlock
                // Validation will happen when CLI is actually used
                return Task.FromResult(candidatePath);
            }
        }
        
        throw new CliExecutableNotFoundException(guiExecutableDirectory);
    }

    public async Task<bool> ValidateCliExecutableAsync(string cliExecutablePath)
    {
        try
        {
            using var process = new Process();
            
            ProcessStartInfo startInfo;
            if (cliExecutablePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"{cliExecutablePath} --version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }
            else
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = cliExecutablePath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }
            
            process.StartInfo = startInfo;

            process.Start();

            // Wait for process completion with timeout
            var completed = await WaitForExitAsync(process, TimeSpan.FromSeconds(5));
            
            if (!completed)
            {
                process.Kill();
                return false;
            }

            // CLI should exit successfully and produce some output for --version
            var output = await process.StandardOutput.ReadToEndAsync();
            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
        }
        catch
        {
            // Any exception during validation means the executable is not functional
            return false;
        }
    }

    private static string GetGuiExecutableDirectory()
    {
        // Use AppContext.BaseDirectory which points to the application's directory
        // This works correctly for both single-file apps and regular deployments
        return AppContext.BaseDirectory;
    }

    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        process.EnableRaisingEvents = true;
        process.Exited += (sender, args) => tcs.TrySetResult(true);
        
        if (process.HasExited)
        {
            return true;
        }

        var timeoutTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
        
        return completedTask == tcs.Task;
    }
}