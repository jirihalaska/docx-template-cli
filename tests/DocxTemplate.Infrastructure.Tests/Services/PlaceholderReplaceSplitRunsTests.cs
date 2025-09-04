using DocxTemplate.Core.ErrorHandling;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Services;
using DocxTemplate.Infrastructure.Services;
using DocxTemplate.Infrastructure.DocxProcessing;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocxTemplate.Infrastructure.Tests.Services;

/// <summary>
/// Tests for handling placeholders that are split across multiple runs in Word documents.
/// This addresses the bug where placeholders like {{MISTO_PLNENI}} and {{DELKA_CINNOSTI}}
/// are discovered but not replaced when split across runs.
/// </summary>
public class PlaceholderReplaceSplitRunsTests
{
    private readonly Mock<ILogger<PlaceholderScanService>> _mockScanLogger;
    private readonly Mock<ILogger<PlaceholderReplaceService>> _mockReplaceLogger;
    private readonly Mock<ITemplateDiscoveryService> _mockDiscoveryService;
    private readonly Mock<IErrorHandler> _mockErrorHandler;
    private readonly Mock<IFileSystemService> _mockFileSystemService;
    private readonly Mock<IImageProcessor> _mockImageProcessor;
    private readonly DocumentTraverser _documentTraverser;
    private readonly PlaceholderScanService _scanService;
    private readonly PlaceholderReplaceService _replaceService;

    public PlaceholderReplaceSplitRunsTests()
    {
        _mockScanLogger = new Mock<ILogger<PlaceholderScanService>>();
        _mockReplaceLogger = new Mock<ILogger<PlaceholderReplaceService>>();
        _mockDiscoveryService = new Mock<ITemplateDiscoveryService>();
        _mockErrorHandler = new Mock<IErrorHandler>();
        _mockFileSystemService = new Mock<IFileSystemService>();
        _mockImageProcessor = new Mock<IImageProcessor>();
        
        var mockTraverserLogger = new Mock<ILogger<DocumentTraverser>>();
        _documentTraverser = new DocumentTraverser(mockTraverserLogger.Object);
        
        _scanService = new PlaceholderScanService(
            _mockDiscoveryService.Object,
            _mockScanLogger.Object,
            _documentTraverser);
            
        _replaceService = new PlaceholderReplaceService(
            _mockReplaceLogger.Object,
            _mockErrorHandler.Object,
            _mockFileSystemService.Object,
            _mockImageProcessor.Object,
            _documentTraverser);
    }

    [Fact]
    public async Task SplitPlaceholders_MISTO_PLNENI_And_DELKA_CINNOSTI_ShouldBeDiscoveredAndReplaced()
    {
        // arrange
        var testDocxPath = Path.Combine(Path.GetTempPath(), $"split_placeholders_test_{Guid.NewGuid()}.docx");
        
        try
        {
            // Create document with split placeholders
            CreateDocumentWithSplitPlaceholders(testDocxPath);
            
            // Setup mocks
            _mockFileSystemService.Setup(fs => fs.FileExists(testDocxPath)).Returns(true);
            _mockFileSystemService.Setup(fs => fs.GetFileSize(testDocxPath)).Returns(1024);
            
            var replacementMap = new ReplacementMap
            {
                Mappings = new Dictionary<string, string>
                {
                    { "MISTO_PLNENI", "Prague, Czech Republic" },
                    { "DELKA_CINNOSTI", "24 months" }
                }
            };
            
            // act - first discover placeholders
            var discoveredPlaceholders = await _scanService.ScanSingleFileAsync(testDocxPath);
            
            // assert - verify both placeholders are discovered
            Assert.Contains(discoveredPlaceholders, p => p.Name == "MISTO_PLNENI");
            Assert.Contains(discoveredPlaceholders, p => p.Name == "DELKA_CINNOSTI");
            
            var mistoPlaceholder = discoveredPlaceholders.First(p => p.Name == "MISTO_PLNENI");
            var delkaPlaceholder = discoveredPlaceholders.First(p => p.Name == "DELKA_CINNOSTI");
            
            Assert.True(mistoPlaceholder.TotalOccurrences > 0);
            Assert.True(delkaPlaceholder.TotalOccurrences > 0);
            
            // act - now replace placeholders
            var replaceResult = await _replaceService.ReplacePlaceholdersInFileAsync(testDocxPath, replacementMap, false);
            
            // assert - verify replacement was successful
            Assert.True(replaceResult.IsSuccess, $"Replacement failed: {replaceResult.ErrorMessage}");
            Assert.True(replaceResult.ReplacementCount >= 2, $"Expected at least 2 replacements, got {replaceResult.ReplacementCount}");
            
            // act - scan again after replacement to verify placeholders are gone
            var remainingPlaceholders = await _scanService.ScanSingleFileAsync(testDocxPath);
            
            // assert - verify placeholders are no longer found (they've been replaced)
            Assert.DoesNotContain(remainingPlaceholders, p => p.Name == "MISTO_PLNENI");
            Assert.DoesNotContain(remainingPlaceholders, p => p.Name == "DELKA_CINNOSTI");
            
            // Verify the actual content was replaced
            VerifyPlaceholdersWereReplaced(testDocxPath, replacementMap);
        }
        finally
        {
            if (File.Exists(testDocxPath)) File.Delete(testDocxPath);
        }
    }

    [Fact]
    public async Task SplitPlaceholder_AcrossThreeRuns_ShouldBeDiscoveredAndReplaced()
    {
        // arrange
        var testDocxPath = Path.Combine(Path.GetTempPath(), $"three_run_split_{Guid.NewGuid()}.docx");
        
        try
        {
            // Create document with placeholder split across three runs
            CreateDocumentWithThreeRunSplitPlaceholder(testDocxPath);
            
            _mockFileSystemService.Setup(fs => fs.FileExists(testDocxPath)).Returns(true);
            _mockFileSystemService.Setup(fs => fs.GetFileSize(testDocxPath)).Returns(1024);
            
            var replacementMap = new ReplacementMap
            {
                Mappings = new Dictionary<string, string>
                {
                    { "TEST_PLACEHOLDER", "Replacement Value" }
                }
            };
            
            // act - discover placeholders
            var discoveredPlaceholders = await _scanService.ScanSingleFileAsync(testDocxPath);
            
            // assert - verify placeholder is discovered even when split across three runs
            Assert.Contains(discoveredPlaceholders, p => p.Name == "TEST_PLACEHOLDER");
            
            // act - replace placeholders
            var replaceResult = await _replaceService.ReplacePlaceholdersInFileAsync(testDocxPath, replacementMap, false);
            
            // assert - verify replacement was successful
            Assert.True(replaceResult.IsSuccess, $"Replacement failed: {replaceResult.ErrorMessage}");
            Assert.True(replaceResult.ReplacementCount >= 1, $"Expected at least 1 replacement, got {replaceResult.ReplacementCount}");
            
            // act - scan again to verify placeholder is gone
            var remainingPlaceholders = await _scanService.ScanSingleFileAsync(testDocxPath);
            
            // assert - verify placeholder is no longer found
            Assert.DoesNotContain(remainingPlaceholders, p => p.Name == "TEST_PLACEHOLDER");
            
            // Verify the actual content was replaced
            VerifyPlaceholdersWereReplaced(testDocxPath, replacementMap);
        }
        finally
        {
            if (File.Exists(testDocxPath)) File.Delete(testDocxPath);
        }
    }

    [Fact]
    public async Task MultipleSplitPlaceholders_InSameParagraph_ShouldAllBeReplaced()
    {
        // arrange
        var testDocxPath = Path.Combine(Path.GetTempPath(), $"multiple_split_{Guid.NewGuid()}.docx");
        
        try
        {
            // Create document with multiple split placeholders in same paragraph
            CreateDocumentWithMultipleSplitPlaceholders(testDocxPath);
            
            _mockFileSystemService.Setup(fs => fs.FileExists(testDocxPath)).Returns(true);
            _mockFileSystemService.Setup(fs => fs.GetFileSize(testDocxPath)).Returns(1024);
            
            var replacementMap = new ReplacementMap
            {
                Mappings = new Dictionary<string, string>
                {
                    { "FIRST", "First Value" },
                    { "SECOND", "Second Value" },
                    { "THIRD", "Third Value" }
                }
            };
            
            // act - discover placeholders
            var discoveredPlaceholders = await _scanService.ScanSingleFileAsync(testDocxPath);
            
            // assert - verify all placeholders are discovered
            Assert.Contains(discoveredPlaceholders, p => p.Name == "FIRST");
            Assert.Contains(discoveredPlaceholders, p => p.Name == "SECOND");
            Assert.Contains(discoveredPlaceholders, p => p.Name == "THIRD");
            
            // act - replace placeholders
            var replaceResult = await _replaceService.ReplacePlaceholdersInFileAsync(testDocxPath, replacementMap, false);
            
            // assert - verify all replacements were successful
            Assert.True(replaceResult.IsSuccess, $"Replacement failed: {replaceResult.ErrorMessage}");
            Assert.True(replaceResult.ReplacementCount >= 3, $"Expected at least 3 replacements, got {replaceResult.ReplacementCount}");
            
            // act - scan again to verify all placeholders are gone
            var remainingPlaceholders = await _scanService.ScanSingleFileAsync(testDocxPath);
            
            // assert - verify no placeholders remain
            Assert.DoesNotContain(remainingPlaceholders, p => p.Name == "FIRST");
            Assert.DoesNotContain(remainingPlaceholders, p => p.Name == "SECOND");
            Assert.DoesNotContain(remainingPlaceholders, p => p.Name == "THIRD");
            
            // Verify the actual content was replaced
            VerifyPlaceholdersWereReplaced(testDocxPath, replacementMap);
        }
        finally
        {
            if (File.Exists(testDocxPath)) File.Delete(testDocxPath);
        }
    }

    private static void CreateDocumentWithSplitPlaceholders(string docxPath)
    {
        using var doc = WordprocessingDocument.Create(docxPath, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        var document = new Document();
        var body = new Body();
        
        // Create paragraph with MISTO_PLNENI split across runs
        var paragraph1 = new Paragraph();
        paragraph1.AppendChild(new Run(new Text("This is the location: ")));
        paragraph1.AppendChild(new Run(new Text("{{MIST"))); // Split here
        paragraph1.AppendChild(new Run(new Text("O_PLNENI}}"))); // Continuation
        paragraph1.AppendChild(new Run(new Text(" and some more text.")));
        
        // Create paragraph with DELKA_CINNOSTI split across runs
        var paragraph2 = new Paragraph();
        paragraph2.AppendChild(new Run(new Text("Duration: ")));
        paragraph2.AppendChild(new Run(new Text("{{DEL"))); // Split here
        paragraph2.AppendChild(new Run(new Text("KA_CINNO"))); // Middle
        paragraph2.AppendChild(new Run(new Text("STI}}"))); // End
        paragraph2.AppendChild(new Run(new Text(" total.")));
        
        body.AppendChild(paragraph1);
        body.AppendChild(paragraph2);
        document.AppendChild(body);
        mainPart.Document = document;
        mainPart.Document.Save();
    }

    private static void CreateDocumentWithThreeRunSplitPlaceholder(string docxPath)
    {
        using var doc = WordprocessingDocument.Create(docxPath, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        var document = new Document();
        var body = new Body();
        
        // Create paragraph with placeholder split across three runs
        var paragraph = new Paragraph();
        paragraph.AppendChild(new Run(new Text("Value: ")));
        paragraph.AppendChild(new Run(new Text("{{TEST_"))); // First part
        paragraph.AppendChild(new Run(new Text("PLACE"))); // Second part
        paragraph.AppendChild(new Run(new Text("HOLDER}}"))); // Third part
        paragraph.AppendChild(new Run(new Text(" end.")));
        
        body.AppendChild(paragraph);
        document.AppendChild(body);
        mainPart.Document = document;
        mainPart.Document.Save();
    }

    private static void CreateDocumentWithMultipleSplitPlaceholders(string docxPath)
    {
        using var doc = WordprocessingDocument.Create(docxPath, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        var document = new Document();
        var body = new Body();
        
        // Create paragraph with multiple split placeholders
        var paragraph = new Paragraph();
        paragraph.AppendChild(new Run(new Text("Start: ")));
        paragraph.AppendChild(new Run(new Text("{{FIR"))); // Split FIRST
        paragraph.AppendChild(new Run(new Text("ST}} middle ")));
        paragraph.AppendChild(new Run(new Text("{{SEC"))); // Split SECOND
        paragraph.AppendChild(new Run(new Text("OND}} between ")));
        paragraph.AppendChild(new Run(new Text("{{THI"))); // Split THIRD
        paragraph.AppendChild(new Run(new Text("RD}} end.")));
        
        body.AppendChild(paragraph);
        document.AppendChild(body);
        mainPart.Document = document;
        mainPart.Document.Save();
    }

    private static void VerifyPlaceholdersWereReplaced(string docxPath, ReplacementMap replacementMap)
    {
        using var doc = WordprocessingDocument.Open(docxPath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        Assert.NotNull(body);
        
        // Get all text from the document
        var allText = string.Join("", body.Descendants<Text>().Select(t => t.Text));
        
        // Verify that replacement values are present
        foreach (var mapping in replacementMap.Mappings)
        {
            Assert.Contains(mapping.Value, allText);
            
            // Verify that the original placeholder is NOT present
            var placeholderText = $"{{{{{mapping.Key}}}}}";
            Assert.DoesNotContain(placeholderText, allText);
        }
    }
}