using System.CommandLine;
using DocxTemplate.CLI.Commands;
using Xunit;

namespace DocxTemplate.CLI.Tests.Commands;

public class CopyCommandTests
{
    private readonly CopyCommand _command;

    public CopyCommandTests()
    {
        _command = new CopyCommand();
    }

    [Fact]
    public void Constructor_CreatesCommandWithCorrectName()
    {
        // arrange & act
        var command = new CopyCommand();

        // assert
        Assert.Equal("copy", command.Name);
        Assert.Equal("Copy DOCX templates to target directory", command.Description);
    }

    [Fact]
    public void Constructor_AddsRequiredOptions()
    {
        // arrange & act
        var command = new CopyCommand();

        // assert
        Assert.Contains(command.Options, o => o.Name == "source");
        Assert.Contains(command.Options, o => o.Name == "target");
        Assert.Contains(command.Options, o => o.Name == "preserve-structure");
        Assert.Contains(command.Options, o => o.Name == "overwrite");
        Assert.Contains(command.Options, o => o.Name == "dry-run");
        Assert.Contains(command.Options, o => o.Name == "format");
        Assert.Contains(command.Options, o => o.Name == "quiet");
        Assert.Contains(command.Options, o => o.Name == "validate");
        Assert.Contains(command.Options, o => o.Name == "estimate");
    }

    [Fact]
    public void SourceOption_IsRequired()
    {
        // arrange & act
        var sourceOption = _command.Options.FirstOrDefault(o => o.Name == "source");

        // assert
        Assert.NotNull(sourceOption);
        Assert.True(sourceOption.IsRequired);
    }

    [Fact]
    public void TargetOption_IsRequired()
    {
        // arrange & act
        var targetOption = _command.Options.FirstOrDefault(o => o.Name == "target");

        // assert
        Assert.NotNull(targetOption);
        Assert.True(targetOption.IsRequired);
    }

    [Fact]
    public void PreserveStructureOption_HasDefaultValue()
    {
        // arrange & act
        var preserveStructureOption = _command.Options.FirstOrDefault(o => o.Name == "preserve-structure");

        // assert
        Assert.NotNull(preserveStructureOption);
        Assert.False(preserveStructureOption.IsRequired);
    }

    [Fact]
    public void OverwriteOption_HasDefaultValue()
    {
        // arrange & act
        var overwriteOption = _command.Options.FirstOrDefault(o => o.Name == "overwrite");

        // assert
        Assert.NotNull(overwriteOption);
        Assert.False(overwriteOption.IsRequired);
    }

    [Fact]
    public void DryRunOption_HasDefaultValue()
    {
        // arrange & act
        var dryRunOption = _command.Options.FirstOrDefault(o => o.Name == "dry-run");

        // assert
        Assert.NotNull(dryRunOption);
        Assert.False(dryRunOption.IsRequired);
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

    [Fact]
    public void QuietOption_HasDefaultValue()
    {
        // arrange & act
        var quietOption = _command.Options.FirstOrDefault(o => o.Name == "quiet");

        // assert
        Assert.NotNull(quietOption);
        Assert.False(quietOption.IsRequired);
    }

    [Fact]
    public void ValidateOption_HasDefaultValue()
    {
        // arrange & act
        var validateOption = _command.Options.FirstOrDefault(o => o.Name == "validate");

        // assert
        Assert.NotNull(validateOption);
        Assert.False(validateOption.IsRequired);
    }

    [Fact]
    public void EstimateOption_HasDefaultValue()
    {
        // arrange & act
        var estimateOption = _command.Options.FirstOrDefault(o => o.Name == "estimate");

        // assert
        Assert.NotNull(estimateOption);
        Assert.False(estimateOption.IsRequired);
    }

    [Fact]
    public void OverwriteOption_HasForceAlias()
    {
        // arrange & act
        var overwriteOption = _command.Options.FirstOrDefault(o => o.Name == "overwrite");

        // assert
        Assert.NotNull(overwriteOption);
        Assert.Contains("--force", overwriteOption.Aliases);
        Assert.Contains("-f", overwriteOption.Aliases);
    }

    [Fact]
    public void SourceOption_HasShortAlias()
    {
        // arrange & act
        var sourceOption = _command.Options.FirstOrDefault(o => o.Name == "source");

        // assert
        Assert.NotNull(sourceOption);
        Assert.Contains("-s", sourceOption.Aliases);
    }

    [Fact]
    public void TargetOption_HasShortAlias()
    {
        // arrange & act
        var targetOption = _command.Options.FirstOrDefault(o => o.Name == "target");

        // assert
        Assert.NotNull(targetOption);
        Assert.Contains("-t", targetOption.Aliases);
    }

    [Fact]
    public void PreserveStructureOption_HasShortAlias()
    {
        // arrange & act
        var preserveStructureOption = _command.Options.FirstOrDefault(o => o.Name == "preserve-structure");

        // assert
        Assert.NotNull(preserveStructureOption);
        Assert.Contains("-p", preserveStructureOption.Aliases);
    }

    [Fact]
    public void DryRunOption_HasShortAlias()
    {
        // arrange & act
        var dryRunOption = _command.Options.FirstOrDefault(o => o.Name == "dry-run");

        // assert
        Assert.NotNull(dryRunOption);
        Assert.Contains("-d", dryRunOption.Aliases);
    }

    [Fact]
    public void FormatOption_HasShortAlias()
    {
        // arrange & act
        var formatOption = _command.Options.FirstOrDefault(o => o.Name == "format");

        // assert
        Assert.NotNull(formatOption);
        Assert.Contains("-o", formatOption.Aliases);
    }

    [Fact]
    public void QuietOption_HasShortAlias()
    {
        // arrange & act
        var quietOption = _command.Options.FirstOrDefault(o => o.Name == "quiet");

        // assert
        Assert.NotNull(quietOption);
        Assert.Contains("-q", quietOption.Aliases);
    }

    [Fact]
    public void ValidateOption_HasShortAlias()
    {
        // arrange & act
        var validateOption = _command.Options.FirstOrDefault(o => o.Name == "validate");

        // assert
        Assert.NotNull(validateOption);
        Assert.Contains("-v", validateOption.Aliases);
    }

    [Fact]
    public void EstimateOption_HasShortAlias()
    {
        // arrange & act
        var estimateOption = _command.Options.FirstOrDefault(o => o.Name == "estimate");

        // assert
        Assert.NotNull(estimateOption);
        Assert.Contains("-e", estimateOption.Aliases);
    }
}