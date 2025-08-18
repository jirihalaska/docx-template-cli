using DocxTemplate.UI.Services;
using FluentAssertions;
using Xunit;

namespace DocxTemplate.UI.Tests.Services;

public class CliCommandBuilderPathQuotingTests
{
    private readonly CliCommandBuilder _builder = new();

    [Fact]
    public void BuildCopyCommand_WithSimplePaths_ShouldNotQuote()
    {
        // arrange
        var source = "/simple/path";
        var target = "/another/path";

        // act
        var result = _builder.BuildCopyCommand(source, target);

        // assert
        result.Arguments.Should().Contain("--source");
        result.Arguments.Should().Contain("/simple/path");
        result.Arguments.Should().Contain("--target");
        result.Arguments.Should().Contain("/another/path");
    }

    [Fact]
    public void BuildCopyCommand_WithPathsContainingSpaces_ShouldQuote()
    {
        // arrange
        var source = "/path with spaces/source";
        var target = "/target with spaces";

        // act
        var result = _builder.BuildCopyCommand(source, target);

        // assert
        result.Arguments.Should().Contain("--source");
        result.Arguments.Should().Contain("\"/path with spaces/source\"");
        result.Arguments.Should().Contain("--target");
        result.Arguments.Should().Contain("\"/target with spaces\"");
    }

    [Fact]
    public void BuildCopyCommand_WithAlreadyQuotedPaths_ShouldNotDoubleQuote()
    {
        // arrange
        var source = "\"/already/quoted path\"";
        var target = "\"/already quoted target\"";

        // act
        var result = _builder.BuildCopyCommand(source, target);

        // assert
        result.Arguments.Should().Contain("--source");
        result.Arguments.Should().Contain("\"/already/quoted path\"");
        result.Arguments.Should().Contain("--target");
        result.Arguments.Should().Contain("\"/already quoted target\"");
    }

    [Fact]
    public void BuildReplaceCommand_WithPathsContainingSpaces_ShouldQuote()
    {
        // arrange
        var folder = "/folder with spaces";
        var mapFile = "/map file with spaces.json";

        // act
        var result = _builder.BuildReplaceCommand(folder, mapFile);

        // assert
        result.Arguments.Should().Contain("--folder");
        result.Arguments.Should().Contain("\"/folder with spaces\"");
        result.Arguments.Should().Contain("--map");
        result.Arguments.Should().Contain("\"/map file with spaces.json\"");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void BuildCopyCommand_WithNullOrEmptyPaths_ShouldHandleGracefully(string? path)
    {
        // act & assert - Should not throw
        var result = _builder.BuildCopyCommand(path!, "/target");
        result.Should().NotBeNull();
    }
}