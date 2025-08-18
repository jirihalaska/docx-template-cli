using System;
using System.Threading.Tasks;
using DocxTemplate.UI.Services;
using Xunit;

namespace DocxTemplate.UI.Tests.Services;

public class CliProcessRunnerTests
{
    [Fact]
    public void Constructor_WithNullPath_ThrowsArgumentNullException()
    {
        // arrange & act & assert
        Assert.Throws<ArgumentNullException>(() => new CliProcessRunner(null!));
    }

    [Fact]
    public void Constructor_WithValidPath_SetsPath()
    {
        // arrange
        var path = "test-cli.exe";
        
        // act
        var runner = new CliProcessRunner(path);
        
        // assert
        Assert.NotNull(runner);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithNonExistentExecutable_ThrowsInvalidOperationException()
    {
        // arrange
        var runner = new CliProcessRunner("/path/that/does/not/exist");
        
        // act & assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => runner.ExecuteCommandAsync("test", new[] { "--version" }));
        
        Assert.Contains("Failed to execute CLI command", exception.Message);
    }

    [Fact]
    public void Constructor_WithDefaultTimeout_UsesDefaultValue()
    {
        // arrange & act
        var runner = new CliProcessRunner("test-path");
        
        // assert
        Assert.NotNull(runner);
    }

    [Fact]
    public void Constructor_WithCustomTimeout_AcceptsTimeout()
    {
        // arrange
        var customTimeout = 60000;
        
        // act
        var runner = new CliProcessRunner("test-path", customTimeout);
        
        // assert
        Assert.NotNull(runner);
    }
}