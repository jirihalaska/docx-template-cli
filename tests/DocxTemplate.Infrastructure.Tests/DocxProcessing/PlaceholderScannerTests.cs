using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxTemplate.Infrastructure.DocxProcessing;
using DocxTemplate.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocxTemplate.Infrastructure.Tests.DocxProcessing;

public class PlaceholderScannerTests : IDisposable
{
    private readonly PlaceholderScanner _scanner;
    private readonly Mock<ILogger<PlaceholderScanner>> _loggerMock;
    private readonly Mock<PlaceholderReplacementEngine> _replacementEngineMock;
    private readonly string _tempFilePath;

    public PlaceholderScannerTests()
    {
        _loggerMock = new Mock<ILogger<PlaceholderScanner>>();
        _replacementEngineMock = new Mock<PlaceholderReplacementEngine>(
            Mock.Of<ILogger<PlaceholderReplacementEngine>>(),
            Mock.Of<IImageProcessor>());
        _scanner = new PlaceholderScanner(_loggerMock.Object, _replacementEngineMock.Object);
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.docx");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public async Task ScanDocumentElementAsync_DetectsCompleteImagePlaceholder()
    {
        // arrange
        var document = CreateDocumentWithText("Here is an {{image:logo|width:200|height:100}} placeholder");
        
        // act
        var result = await _scanner.ScanDocumentElementAsync(
            document.Body,
            _tempFilePath,
            "Body",
            CancellationToken.None);
        
        // assert
        Assert.Single(result);
        Assert.Contains("image:logo|width:200|height:100", result.Keys);
        var locations = result["image:logo|width:200|height:100"];
        Assert.Single(locations);
        Assert.Equal(1, locations[0].Occurrences);
    }

    [Fact]
    public async Task ScanDocumentElementAsync_DetectsSplitImagePlaceholder()
    {
        // arrange
        // Create a document where the placeholder is split across multiple runs
        var document = CreateDocumentWithSplitText(
            "Here is an {{image:",
            "logo|width:200",
            "|height:100}} placeholder");
        
        // act
        var result = await _scanner.ScanDocumentElementAsync(
            document.Body,
            _tempFilePath,
            "Body",
            CancellationToken.None);
        
        // assert
        Assert.Single(result);
        Assert.Contains("image:logo|width:200|height:100", result.Keys);
    }

    [Fact]
    public async Task ScanDocumentElementAsync_DetectsMixedPlaceholders()
    {
        // arrange
        var document = CreateDocumentWithText(
            "Text placeholder {{name}} and image {{image:photo|width:300|height:200}} together");
        
        // act
        var result = await _scanner.ScanDocumentElementAsync(
            document.Body,
            _tempFilePath,
            "Body",
            CancellationToken.None);
        
        // assert
        Assert.Equal(2, result.Count);
        Assert.Contains("name", result.Keys);
        Assert.Contains("image:photo|width:300|height:200", result.Keys);
    }

    [Fact]
    public async Task ScanDocumentElementAsync_HandlesPlaceholderInTable()
    {
        // arrange
        var document = CreateDocumentWithTable("{{image:chart|width:400|height:300}}");
        
        // act
        var result = await _scanner.ScanDocumentElementAsync(
            document.Body,
            _tempFilePath,
            "Body",
            CancellationToken.None);
        
        // assert
        Assert.Single(result);
        Assert.Contains("image:chart|width:400|height:300", result.Keys);
        var locations = result["image:chart|width:400|height:300"];
        // The placeholder is detected in the table context
        Assert.True(locations.Any(l => l.Context.Contains("(Table)")));
    }

    [Fact]
    public void ReconstructParagraphText_HandlesMultipleRuns()
    {
        // arrange
        var paragraph = new Paragraph(
            new Run(new Text("Hello ")),
            new Run(new Text("{{placeholder}}")),
            new Run(new Text(" world")));
        
        // act
        var result = _scanner.ReconstructParagraphText(paragraph);
        
        // assert
        Assert.Equal("Hello {{placeholder}} world", result);
    }

    [Fact]
    public void ReconstructParagraphText_HandlesEmptyParagraph()
    {
        // arrange
        var paragraph = new Paragraph();
        
        // act
        var result = _scanner.ReconstructParagraphText(paragraph);
        
        // assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ReconstructParagraphText_HandlesNullParagraph()
    {
        // arrange
        Paragraph? paragraph = null;
        
        // act
        var result = _scanner.ReconstructParagraphText(paragraph!);
        
        // assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task ScanDocumentElementAsync_IgnoresMalformedImagePlaceholder()
    {
        // arrange
        var document = CreateDocumentWithText(
            "Invalid {{image:logo|width:notanumber|height:100}} placeholder");
        
        // act
        var result = await _scanner.ScanDocumentElementAsync(
            document.Body,
            _tempFilePath,
            "Body",
            CancellationToken.None);
        
        // assert
        // Should be detected as a text placeholder, not an image placeholder
        Assert.Single(result);
        Assert.Contains("image:logo|width:notanumber|height:100", result.Keys);
    }

    [Fact]
    public async Task ScanDocumentElementAsync_HandlesMultipleImagePlaceholders()
    {
        // arrange
        var document = CreateDocumentWithText(
            "First {{image:logo1|width:100|height:50}} and second {{image:logo2|width:200|height:100}}");
        
        // act
        var result = await _scanner.ScanDocumentElementAsync(
            document.Body,
            _tempFilePath,
            "Body",
            CancellationToken.None);
        
        // assert
        Assert.Equal(2, result.Count);
        Assert.Contains("image:logo1|width:100|height:50", result.Keys);
        Assert.Contains("image:logo2|width:200|height:100", result.Keys);
    }

    private Document CreateDocumentWithText(string text)
    {
        return new Document(
            new Body(
                new Paragraph(
                    new Run(
                        new Text(text)))));
    }

    private Document CreateDocumentWithSplitText(params string[] textParts)
    {
        var runs = textParts.Select(text => new Run(new Text(text))).ToArray();
        return new Document(
            new Body(
                new Paragraph(runs)));
    }

    private Document CreateDocumentWithTable(string cellText)
    {
        return new Document(
            new Body(
                new Table(
                    new TableRow(
                        new TableCell(
                            new Paragraph(
                                new Run(
                                    new Text(cellText))))))));
    }
}