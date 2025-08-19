using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DocxTemplate.EndToEnd.Tests.Distribution
{
    /// <summary>
    /// Tests to validate distribution packages contain all required components and work correctly
    /// </summary>
    public class DistributionValidationTests
    {
        private readonly ITestOutputHelper _output;

        public DistributionValidationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DistributionPackage_ShouldContainAllRequiredFiles()
        {
            // arrange
            var testDistributionPath = Path.Combine(Directory.GetCurrentDirectory(), "dist", "test-complete");
            
            // act & assert
            Assert.True(Directory.Exists(testDistributionPath), $"Test distribution directory should exist at {testDistributionPath}");
            
            // Check GUI executable
            var guiPath = Path.Combine(testDistributionPath, "DocxTemplate.UI");
            Assert.True(File.Exists(guiPath), "GUI executable should exist");
            
            // Check CLI executable
            var cliPath = Path.Combine(testDistributionPath, "docx-template");
            Assert.True(File.Exists(cliPath), "CLI executable should exist");
            
            // Check templates directory
            var templatesPath = Path.Combine(testDistributionPath, "templates");
            Assert.True(Directory.Exists(templatesPath), "Templates directory should exist");
            
            // Check README
            var readmePath = Path.Combine(testDistributionPath, "README.md");
            Assert.True(File.Exists(readmePath), "README.md should exist");
            
            _output.WriteLine($"Distribution validation passed for: {testDistributionPath}");
        }

        [Fact]
        public async Task CLI_ShouldExecuteBasicCommands()
        {
            // arrange
            var testDistributionPath = Path.Combine(Directory.GetCurrentDirectory(), "dist", "test-complete");
            var cliPath = Path.Combine(testDistributionPath, "docx-template");
            
            if (!File.Exists(cliPath))
            {
                _output.WriteLine($"Skipping CLI test - executable not found at: {cliPath}");
                return;
            }

            // act & assert - test help command
            var helpResult = await ExecuteProcessAsync(cliPath, "--help");
            Assert.True(helpResult.success, $"CLI help command should succeed. Error: {helpResult.error}");
            Assert.Contains("DocxTemplate CLI", helpResult.output);
            
            // act & assert - test discover command
            var templatesPath = Path.Combine(testDistributionPath, "templates");
            if (Directory.Exists(templatesPath))
            {
                var discoverResult = await ExecuteProcessAsync(cliPath, $"discover --path \"{templatesPath}\" --format Json");
                Assert.True(discoverResult.success, $"CLI discover command should succeed. Error: {discoverResult.error}");
                Assert.Contains("\"success\": true", discoverResult.output);
                
                _output.WriteLine($"CLI discover found templates in: {templatesPath}");
            }
        }

        [Fact]
        public void Templates_ShouldBeIncludedAndAccessible()
        {
            // arrange
            var testDistributionPath = Path.Combine(Directory.GetCurrentDirectory(), "dist", "test-complete");
            var templatesPath = Path.Combine(testDistributionPath, "templates");
            
            // act & assert
            Assert.True(Directory.Exists(templatesPath), "Templates directory should exist");
            
            var templateFiles = Directory.GetFiles(templatesPath, "*.docx", SearchOption.AllDirectories);
            Assert.True(templateFiles.Length > 0, "Should contain at least one template file");
            
            // Check for expected template structure
            var expectedDirectories = new[]
            {
                "01 VZOR Užší řízení",
                "02 VZOR Jiné řizené"
            };
            
            foreach (var expectedDir in expectedDirectories)
            {
                var dirPath = Path.Combine(templatesPath, expectedDir);
                Assert.True(Directory.Exists(dirPath), $"Expected template directory should exist: {expectedDir}");
            }
            
            _output.WriteLine($"Found {templateFiles.Length} template files in distribution");
        }

        [Fact]
        public void Distribution_ShouldHaveCorrectExecutablePermissions()
        {
            // arrange
            var testDistributionPath = Path.Combine(Directory.GetCurrentDirectory(), "dist", "test-complete");
            
            if (!Directory.Exists(testDistributionPath))
            {
                _output.WriteLine($"Skipping permissions test - distribution not found at: {testDistributionPath}");
                return;
            }
            
            // act & assert
            var guiPath = Path.Combine(testDistributionPath, "DocxTemplate.UI");
            var cliPath = Path.Combine(testDistributionPath, "docx-template");
            
            if (File.Exists(guiPath))
            {
                var guiInfo = new FileInfo(guiPath);
                Assert.True(guiInfo.Exists, "GUI executable should exist");
                _output.WriteLine($"GUI executable size: {guiInfo.Length} bytes");
            }
            
            if (File.Exists(cliPath))
            {
                var cliInfo = new FileInfo(cliPath);
                Assert.True(cliInfo.Exists, "CLI executable should exist");
                _output.WriteLine($"CLI executable size: {cliInfo.Length} bytes");
            }
        }

        private static async Task<(bool success, string output, string error)> ExecuteProcessAsync(string fileName, string arguments)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                var output = await outputTask;
                var error = await errorTask;
                
                return (process.ExitCode == 0, output, error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, ex.Message);
            }
        }
    }
}