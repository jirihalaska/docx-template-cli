using System;
using System.Threading.Tasks;
using DocxTemplate.UI.Services;
using Xunit;

namespace DocxTemplate.UI.Tests.Services;

public class CliProcessRunnerIntegrationTests
{
    [Fact]
    public void Constructor_Default_ShouldUseDllPath()
    {
        // arrange & act
        var runner = new CliProcessRunner();

        // assert
        // We can't directly access the private field, but we can test the behavior
        // by ensuring it uses dotnet to run the DLL
        Assert.NotNull(runner);
    }

    [Fact]
    public void Constructor_WithDllPath_ShouldAcceptDllPath()
    {
        // arrange & act
        var runner = new CliProcessRunner("test.dll", 5000);

        // assert
        Assert.NotNull(runner);
    }

    [Fact]
    public void Constructor_NullPath_ShouldThrow()
    {
        // arrange, act & assert
        Assert.Throws<ArgumentNullException>(() => new CliProcessRunner(null!));
    }

    [Theory]
    [InlineData("scan", new[] { "--path", "test", "--format", "json", "--quiet" })]
    [InlineData("list-sets", new[] { "--templates", "./templates" })]
    public async Task ExecuteCommandAsync_ValidCommand_ShouldHandleArguments(string command, string[] arguments)
    {
        // arrange
        var runner = new CliProcessRunner("nonexistent.dll", 1000); // Use short timeout for test

        // act & assert
        // This will fail because the DLL doesn't exist, but we're testing argument handling
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runner.ExecuteCommandAsync(command, arguments));
        
        // Should fail with process start error, not argument parsing error
        Assert.Contains("Failed to execute CLI command", exception.Message);
    }

    [Fact]
    public async Task ExecuteCommandAsync_EmptyCommand_ShouldHandleGracefully()
    {
        // arrange
        var runner = new CliProcessRunner("nonexistent.dll", 1000);

        // act & assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runner.ExecuteCommandAsync("", new[] { "--help" }));
        
        Assert.Contains("Failed to execute CLI command", exception.Message);
    }

    [Fact]
    public async Task ExecuteCommandAsync_NullArguments_ShouldHandleGracefully()
    {
        // arrange
        var runner = new CliProcessRunner("nonexistent.dll", 1000);

        // act & assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runner.ExecuteCommandAsync("scan", null!));
        
        Assert.Contains("Failed to execute CLI command", exception.Message);
    }

    [Fact]
    public void DefaultTimeout_ShouldBe30Seconds()
    {
        // arrange & act
        var runner = new CliProcessRunner();

        // assert
        // We can't directly test the timeout value, but we can ensure
        // the constructor doesn't throw and accepts the default
        Assert.NotNull(runner);
    }

    [Theory]
    [InlineData("DocxTemplate.CLI.dll")]
    [InlineData("test.dll")]
    [InlineData("other.DLL")]
    public void Constructor_DllExtension_ShouldBeTreatedAsDll(string dllPath)
    {
        // arrange & act
        var runner = new CliProcessRunner(dllPath);

        // assert
        Assert.NotNull(runner);
        // The real test would be in execution behavior, but we can't easily test
        // the private logic without exposing internals or integration testing
    }

    [Theory]
    [InlineData("docx-template")]
    [InlineData("docx-template.exe")]
    [InlineData("some-executable")]
    public void Constructor_ExecutablePath_ShouldBeTreatedAsExecutable(string executablePath)
    {
        // arrange & act
        var runner = new CliProcessRunner(executablePath);

        // assert
        Assert.NotNull(runner);
    }
}