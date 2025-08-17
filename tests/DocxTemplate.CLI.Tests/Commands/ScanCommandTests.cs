using System.CommandLine;
using DocxTemplate.CLI.Commands;
using Xunit;

namespace DocxTemplate.CLI.Tests.Commands;

public class ScanCommandTests
{
    private readonly ScanCommand _command;

    public ScanCommandTests()
    {
        _command = new ScanCommand();
    }

    [Fact]
    public void Constructor_CreatesCommandWithCorrectName()
    {
        // arrange & act
        var command = new ScanCommand();

        // assert
        Assert.Equal("scan", command.Name);
        Assert.Equal("Scan DOCX templates for placeholders", command.Description);
    }

    [Fact]
    public void Constructor_AddsRequiredOptions()
    {
        // arrange & act
        var command = new ScanCommand();

        // assert
        Assert.Contains(command.Options, o => o.Name == "path");
        Assert.Contains(command.Options, o => o.Name == "recursive");
        Assert.Contains(command.Options, o => o.Name == "pattern");
        Assert.Contains(command.Options, o => o.Name == "format");
        Assert.Contains(command.Options, o => o.Name == "statistics");
        Assert.Contains(command.Options, o => o.Name == "case-sensitive");
        Assert.Contains(command.Options, o => o.Name == "parallelism");
        Assert.Contains(command.Options, o => o.Name == "quiet");
    }

    [Fact]
    public void PathOption_IsRequired()
    {
        // arrange & act
        var pathOption = _command.Options.FirstOrDefault(o => o.Name == "path");

        // assert
        Assert.NotNull(pathOption);
        Assert.True(pathOption.IsRequired);
    }

    [Fact]
    public void RecursiveOption_HasDefaultValue()
    {
        // arrange & act
        var recursiveOption = _command.Options.FirstOrDefault(o => o.Name == "recursive");

        // assert
        Assert.NotNull(recursiveOption);
        Assert.False(recursiveOption.IsRequired);
    }

    [Fact]
    public void PatternOption_HasDefaultValue()
    {
        // arrange & act
        var patternOption = _command.Options.FirstOrDefault(o => o.Name == "pattern");

        // assert
        Assert.NotNull(patternOption);
        Assert.False(patternOption.IsRequired);
    }

    [Fact]
    public void FormatOption_HasDefaultValue()
    {
        // arrange & act
        var formatOption = _command.Options.FirstOrDefault(o => o.Name == "format");

        // assert
        Assert.NotNull(formatOption);
        Assert.False(formatOption.IsRequired);
    }
}