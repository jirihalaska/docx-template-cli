using DocxTemplate.CLI.Commands;
using FluentAssertions;
using Xunit;

namespace DocxTemplate.CLI.Tests.Commands;

/// <summary>
/// Unit tests for ReplaceCommand class
/// </summary>
public class ReplaceCommandTests
{
    [Fact]
    public void Constructor_SetsCorrectNameAndDescription()
    {
        // arrange & act
        var command = new ReplaceCommand();

        // assert
        command.Name.Should().Be("replace");
        command.Description.Should().Be("Replace placeholders in DOCX templates with actual values");
    }

    [Fact]
    public void Command_HasRequiredFolderOption()
    {
        // arrange
        var command = new ReplaceCommand();

        // act
        var folderOption = command.Options.FirstOrDefault(o => o.Name == "folder");

        // assert
        folderOption.Should().NotBeNull();
        folderOption!.IsRequired.Should().BeTrue();
        folderOption.Aliases.Should().Contain("--folder");
        folderOption.Aliases.Should().Contain("-f");
    }

    [Fact]
    public void Command_HasRequiredMapOption()
    {
        // arrange
        var command = new ReplaceCommand();

        // act
        var mapOption = command.Options.FirstOrDefault(o => o.Name == "map");

        // assert
        mapOption.Should().NotBeNull();
        mapOption!.IsRequired.Should().BeTrue();
        mapOption.Aliases.Should().Contain("--map");
        mapOption.Aliases.Should().Contain("-m");
    }

    [Fact]
    public void Command_HasBackupOption()
    {
        // arrange
        var command = new ReplaceCommand();

        // act
        var backupOption = command.Options.FirstOrDefault(o => o.Name == "backup");

        // assert
        backupOption.Should().NotBeNull();
        backupOption!.IsRequired.Should().BeFalse();
        backupOption.Aliases.Should().Contain("--backup");
        backupOption.Aliases.Should().Contain("-b");
    }

    [Fact]
    public void Command_HasRecursiveOption()
    {
        // arrange
        var command = new ReplaceCommand();

        // act
        var recursiveOption = command.Options.FirstOrDefault(o => o.Name == "recursive");

        // assert
        recursiveOption.Should().NotBeNull();
        recursiveOption!.IsRequired.Should().BeFalse();
        recursiveOption.Aliases.Should().Contain("--recursive");
        recursiveOption.Aliases.Should().Contain("-r");
    }

    [Fact]
    public void Command_HasDryRunOption()
    {
        // arrange
        var command = new ReplaceCommand();

        // act
        var dryRunOption = command.Options.FirstOrDefault(o => o.Name == "dry-run");

        // assert
        dryRunOption.Should().NotBeNull();
        dryRunOption!.IsRequired.Should().BeFalse();
        dryRunOption.Aliases.Should().Contain("--dry-run");
        dryRunOption.Aliases.Should().Contain("-d");
    }

    [Fact]
    public void Command_HasFormatOption()
    {
        // arrange
        var command = new ReplaceCommand();

        // act
        var formatOption = command.Options.FirstOrDefault(o => o.Name == "format");

        // assert
        formatOption.Should().NotBeNull();
        formatOption!.IsRequired.Should().BeFalse();
        formatOption.Aliases.Should().Contain("--format");
        formatOption.Aliases.Should().Contain("-o");
    }

    [Fact]
    public void Command_HasQuietOption()
    {
        // arrange
        var command = new ReplaceCommand();

        // act
        var quietOption = command.Options.FirstOrDefault(o => o.Name == "quiet");

        // assert
        quietOption.Should().NotBeNull();
        quietOption!.IsRequired.Should().BeFalse();
        quietOption.Aliases.Should().Contain("--quiet");
        quietOption.Aliases.Should().Contain("-q");
    }

    [Fact]
    public void Command_HasPatternOption()
    {
        // arrange
        var command = new ReplaceCommand();

        // act
        var patternOption = command.Options.FirstOrDefault(o => o.Name == "pattern");

        // assert
        patternOption.Should().NotBeNull();
        patternOption!.IsRequired.Should().BeFalse();
        patternOption.Aliases.Should().Contain("--pattern");
        patternOption.Aliases.Should().Contain("-p");
    }

    [Fact]
    public void Command_HasAllRequiredOptions()
    {
        // arrange
        var command = new ReplaceCommand();

        // act
        var optionNames = command.Options.Select(o => o.Name).ToList();

        // assert
        optionNames.Should().Contain("folder");
        optionNames.Should().Contain("map");
        optionNames.Should().Contain("backup");
        optionNames.Should().Contain("recursive");
        optionNames.Should().Contain("dry-run");
        optionNames.Should().Contain("format");
        optionNames.Should().Contain("quiet");
        optionNames.Should().Contain("pattern");
    }

    [Fact]
    public void Command_HasCorrectRequiredOptions()
    {
        // arrange
        var command = new ReplaceCommand();

        // act
        var requiredOptions = command.Options.Where(o => o.IsRequired).Select(o => o.Name).ToList();

        // assert
        requiredOptions.Should().HaveCount(2);
        requiredOptions.Should().Contain("folder");
        requiredOptions.Should().Contain("map");
    }

    [Theory]
    [InlineData("folder", "--folder", "-f")]
    [InlineData("map", "--map", "-m")]
    [InlineData("backup", "--backup", "-b")]
    [InlineData("recursive", "--recursive", "-r")]
    [InlineData("dry-run", "--dry-run", "-d")]
    [InlineData("format", "--format", "-o")]
    [InlineData("quiet", "--quiet", "-q")]
    [InlineData("pattern", "--pattern", "-p")]
    public void Command_OptionHasCorrectAliases(string optionName, string longAlias, string shortAlias)
    {
        // arrange
        var command = new ReplaceCommand();

        // act
        var option = command.Options.FirstOrDefault(o => o.Name == optionName);

        // assert
        option.Should().NotBeNull();
        option!.Aliases.Should().Contain(longAlias);
        option.Aliases.Should().Contain(shortAlias);
    }

    [Fact]
    public void Command_InheritsFromSystemCommandLineCommand()
    {
        // arrange & act
        var command = new ReplaceCommand();

        // assert
        command.Should().BeAssignableTo<System.CommandLine.Command>();
    }
}