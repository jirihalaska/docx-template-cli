using DocxTemplate.UI.Services;

namespace DocxTemplate.UI.Tests.Services;

public class CliCommandBuilderTests
{
    private readonly CliCommandBuilder _builder = new();

    [Fact]
    public void BuildListSetsCommand_ShouldReturnCorrectFormat()
    {
        // arrange
        var templatesPath = "./templates";

        // act
        var result = _builder.BuildListSetsCommand(templatesPath);

        // assert
        Assert.Equal("list-sets --templates \"./templates\" --format json", result);
    }

    [Fact]
    public void BuildScanCommand_ShouldReturnCorrectFormat()
    {
        // arrange
        var path = "./templates/contracts";

        // act
        var result = _builder.BuildScanCommand(path);

        // assert
        Assert.Equal("scan --path \"./templates/contracts\" --format json", result);
    }

    [Fact]
    public void BuildCopyCommand_ShouldReturnCorrectFormat()
    {
        // arrange
        var source = "./templates/contracts";
        var target = "./output";

        // act
        var result = _builder.BuildCopyCommand(source, target);

        // assert
        Assert.Equal("copy --source \"./templates/contracts\" --target \"./output\" --format json", result);
    }

    [Fact]
    public void BuildReplaceCommand_ShouldReturnCorrectFormat()
    {
        // arrange
        var folder = "./output";
        var mapFile = "./values.json";

        // act
        var result = _builder.BuildReplaceCommand(folder, mapFile);

        // assert
        Assert.Equal("replace --folder \"./output\" --map \"./values.json\" --format json", result);
    }
}