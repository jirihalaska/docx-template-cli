using DocxTemplate.Core.Services;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Models.Results;
using DocxTemplate.TestUtilities;
using FluentAssertions;

namespace DocxTemplate.Core.Tests.Services;

public class IPlaceholderReplaceServiceTests
{
    [Fact]
    public void ReplaceInTemplateAsync_Should_BeDefinedInInterface()
    {
        // arrange
        var interfaceType = typeof(IPlaceholderReplaceService);

        // act
        var method = interfaceType.GetMethod("ReplaceInTemplateAsync");

        // assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<ReplaceResult>));
    }

    [Fact]
    public void ReplacePlaceholdersAsync_WithFolderPath_Should_BeDefinedInInterface()
    {
        // arrange
        var interfaceType = typeof(IPlaceholderReplaceService);

        // act
        var method = interfaceType.GetMethods()
            .FirstOrDefault(m => m.Name == "ReplacePlaceholdersAsync" && 
                                m.GetParameters().Length == 4 &&
                                m.GetParameters()[0].ParameterType == typeof(string));

        // assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<ReplaceResult>));
    }

    [Fact]
    public void ReplacePlaceholdersAsync_WithTemplateFiles_Should_BeDefinedInInterface()
    {
        // arrange
        var interfaceType = typeof(IPlaceholderReplaceService);

        // act
        var method = interfaceType.GetMethods()
            .FirstOrDefault(m => m.Name == "ReplacePlaceholdersAsync" && 
                                m.GetParameters().Length == 4 &&
                                m.GetParameters()[0].ParameterType == typeof(IReadOnlyList<TemplateFile>));

        // assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<ReplaceResult>));
    }

    [Fact]
    public void ReplacePlaceholdersInFileAsync_Should_BeDefinedInInterface()
    {
        // arrange
        var interfaceType = typeof(IPlaceholderReplaceService);

        // act
        var method = interfaceType.GetMethod("ReplacePlaceholdersInFileAsync");

        // assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<FileReplaceResult>));
    }

    [Fact]
    public void PreviewReplacementsAsync_Should_BeDefinedInInterface()
    {
        // arrange
        var interfaceType = typeof(IPlaceholderReplaceService);

        // act
        var method = interfaceType.GetMethod("PreviewReplacementsAsync");

        // assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<ReplacementPreview>));
    }

    [Fact]
    public void ValidateReplacements_Should_BeDefinedInInterface()
    {
        // arrange
        var interfaceType = typeof(IPlaceholderReplaceService);

        // act
        var method = interfaceType.GetMethod("ValidateReplacements");

        // assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(ReplacementValidationResult));
    }

    [Fact]
    public void CreateBackupsAsync_Should_BeDefinedInInterface()
    {
        // arrange
        var interfaceType = typeof(IPlaceholderReplaceService);

        // act
        var method = interfaceType.GetMethod("CreateBackupsAsync");

        // assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<BackupResult>));
    }
}