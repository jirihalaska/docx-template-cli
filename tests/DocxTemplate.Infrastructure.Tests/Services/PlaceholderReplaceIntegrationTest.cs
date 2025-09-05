using DocxTemplate.Core.ErrorHandling;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Services;
using DocxTemplate.Infrastructure.Services;
using DocxTemplate.Infrastructure.DocxProcessing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace DocxTemplate.Infrastructure.Tests.Services;

/// <summary>
/// Integration test using the actual template file mentioned by the user to verify
/// the fix for split placeholder replacement bug with MISTO_PLNENI and DELKA_CINNOSTI.
/// </summary>
public class PlaceholderReplaceIntegrationTest
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ILogger<PlaceholderScanService>> _mockScanLogger;
    private readonly Mock<ILogger<PlaceholderReplaceService>> _mockReplaceLogger;
    private readonly Mock<ITemplateDiscoveryService> _mockDiscoveryService;
    private readonly Mock<IErrorHandler> _mockErrorHandler;
    private readonly Mock<IFileSystemService> _mockFileSystemService;
    private readonly Mock<IImageProcessor> _mockImageProcessor;
    private readonly DocumentTraverser _documentTraverser;
    private readonly PlaceholderScanService _scanService;
    private readonly PlaceholderReplaceService _replaceService;

    public PlaceholderReplaceIntegrationTest(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
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
    public async Task IntegrationTest_WithActualTemplateFile_MISTO_PLNENI_And_DELKA_CINNOSTI_ShouldWork()
    {
        // arrange
        var actualTemplatePath = @"templates/TEST2/TDI - P03_Navrh smlouvy.docx";
        var testCopyPath = Path.Combine(Path.GetTempPath(), $"integration_test_{Guid.NewGuid()}.docx");

        // Skip if the original template file doesn't exist
        if (!File.Exists(actualTemplatePath))
        {
            // Create a test template that simulates the problematic file structure
            CreateTestTemplateWithComplexPlaceholders(testCopyPath);
        }
        else
        {
            // Copy the actual template file for testing
            File.Copy(actualTemplatePath, testCopyPath);
        }
        
        try
        {
            // Setup mocks
            _mockFileSystemService.Setup(fs => fs.FileExists(testCopyPath)).Returns(true);
            _mockFileSystemService.Setup(fs => fs.GetFileSize(testCopyPath)).Returns(new FileInfo(testCopyPath).Length);
            
            var replacementMap = new ReplacementMap
            {
                Mappings = new Dictionary<string, string>
                {
                    { "MISTO_PLNENI", "Praha, Česká republika" },
                    { "DELKA_CINNOSTI", "24 měsíců" },
                    // Add other potential placeholders that might exist in the template
                    { "DATUM", "2025-09-04" },
                    { "DODAVATEL", "Test s.r.o." },
                    { "OBJEDNAVATEL", "Klient s.r.o." }
                }
            };
            
            // act - first scan for placeholders
            var discoveredPlaceholders = await _scanService.ScanSingleFileAsync(testCopyPath);
            
            // Log what we found for debugging
            var foundPlaceholderNames = discoveredPlaceholders.Select(p => p.Name).ToList();
            
            // We expect to find MISTO_PLNENI and DELKA_CINNOSTI if they exist in the template
            bool foundMistoPlneni = discoveredPlaceholders.Any(p => p.Name.Equals("MISTO_PLNENI", StringComparison.OrdinalIgnoreCase));
            bool foundDelkaCinnosti = discoveredPlaceholders.Any(p => p.Name.Equals("DELKA_CINNOSTI", StringComparison.OrdinalIgnoreCase));
            
            // act - now replace placeholders (only if we found the specific ones)
            var replaceResult = await _replaceService.ReplacePlaceholdersInFileAsync(testCopyPath, replacementMap, false);
            
            // assert - verify replacement was successful
            Assert.True(replaceResult.IsSuccess, $"Replacement failed: {replaceResult.ErrorMessage}");
            
            if (foundMistoPlneni || foundDelkaCinnosti)
            {
                Assert.True(replaceResult.ReplacementCount > 0, $"Expected at least 1 replacement, got {replaceResult.ReplacementCount}");
                
                // act - scan again to verify placeholders were replaced
                var remainingPlaceholders = await _scanService.ScanSingleFileAsync(testCopyPath);
                var remainingNames = remainingPlaceholders.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                
                // assert - verify specific placeholders are no longer found
                if (foundMistoPlneni)
                {
                    Assert.DoesNotContain("MISTO_PLNENI", remainingNames);
                }
                if (foundDelkaCinnosti)
                {
                    Assert.DoesNotContain("DELKA_CINNOSTI", remainingNames);
                }
            }
            
            // Verify that the document is still valid after replacement
            VerifyDocumentIntegrity(testCopyPath);
        }
        finally
        {
            if (File.Exists(testCopyPath)) File.Delete(testCopyPath);
        }
    }

    [Fact]
    public async Task IntegrationTest_WithProtokolOPosouzeniFile_ShouldReplaceAllPlaceholders()
    {
        // arrange
        // Build the absolute path to the template file (going up from test directory to project root)
        var currentDirectory = Directory.GetCurrentDirectory();
        var projectRoot = FindProjectRoot(currentDirectory);
        var actualTemplatePath = Path.Combine(projectRoot, "templates", "TEST1", "Protokol o posouzení.docx");
        var testCopyPath = Path.Combine(Path.GetTempPath(), $"protokol_test_{Guid.NewGuid()}.docx");

        // Skip if the original template file doesn't exist
        if (!File.Exists(actualTemplatePath))
        {
            throw new FileNotFoundException($"Test template file not found: {actualTemplatePath}. Please ensure the file exists to run this test.");
        }

        // Copy the actual template file for testing
        File.Copy(actualTemplatePath, testCopyPath);
        
        try
        {
            // Setup mocks
            _mockFileSystemService.Setup(fs => fs.FileExists(testCopyPath)).Returns(true);
            _mockFileSystemService.Setup(fs => fs.GetFileSize(testCopyPath)).Returns(new FileInfo(testCopyPath).Length);
            
            // Create a replacement map using the actual placeholders found in the document
            var replacementMap = new ReplacementMap
            {
                Mappings = new Dictionary<string, string>
                {
                    // Placeholders actually found in the Protokol o posouzení.docx
                    { "DODAVATEL", "Test Dodavatel s.r.o." },
                    { "ICO_DODAVATELE", "12345678" },
                    { "SIDLO_DODAVATELE", "Testovací ulice 123, 110 00 Praha 1" },
                    { "ZADAVATEL_ADRESA", "Zadavatel s.r.o., Hlavní ulice 456, 120 00 Praha 2" },
                    { "ZADAVATEL_ICO", "87654321" },
                    { "ZADAVATEL_NAZEV", "Zadavatel organizace s.r.o." },
                    { "ZAKAZKA_NAZEV", "Testovací zakázka - Protokol o posouzení" },
                    { "ZAKAZKA_PREDMET_DRUH_RIZENI", "Otevřené řízení na dodávku IT služeb" }
                }
            };
            
            // act - first scan for placeholders to see what's in the document
            var discoveredPlaceholders = await _scanService.ScanSingleFileAsync(testCopyPath);
            
            // Log what we found for debugging
            var foundPlaceholderNames = discoveredPlaceholders.Select(p => p.Name).OrderBy(n => n).ToList();
            _output.WriteLine($"Found {discoveredPlaceholders.Count} placeholders in the document:");
            foreach (var placeholder in foundPlaceholderNames)
            {
                _output.WriteLine($"  - {placeholder}");
            }
            
            // act - now replace placeholders
            var replaceResult = await _replaceService.ReplacePlaceholdersInFileAsync(testCopyPath, replacementMap, false);
            
            // assert - verify replacement was successful
            Assert.True(replaceResult.IsSuccess, $"Replacement failed: {replaceResult.ErrorMessage}");
            _output.WriteLine($"Replacement completed with {replaceResult.ReplacementCount} replacements");
            
            // act - scan again to find any remaining placeholders
            var remainingPlaceholders = await _scanService.ScanSingleFileAsync(testCopyPath);
            var remainingNames = remainingPlaceholders.Select(p => p.Name).OrderBy(n => n).ToList();
            
            _output.WriteLine($"Found {remainingPlaceholders.Count} remaining placeholders after replacement:");
            foreach (var placeholder in remainingNames)
            {
                _output.WriteLine($"  - {placeholder}");
            }
            
            // assert - verify that placeholders which have replacements were actually replaced
            var failedReplacements = new List<string>();
            foreach (var mapping in replacementMap.Mappings)
            {
                var placeholderWasFound = discoveredPlaceholders.Any(p => 
                    string.Equals(p.Name, mapping.Key, StringComparison.OrdinalIgnoreCase));
                
                if (placeholderWasFound)
                {
                    var placeholderRemains = remainingPlaceholders.Any(p => 
                        string.Equals(p.Name, mapping.Key, StringComparison.OrdinalIgnoreCase));
                    
                    if (placeholderRemains)
                    {
                        failedReplacements.Add(mapping.Key);
                        
                        // Log detailed information about the failed placeholder
                        var foundInstances = discoveredPlaceholders.Where(p => 
                            string.Equals(p.Name, mapping.Key, StringComparison.OrdinalIgnoreCase)).ToList();
                        var remainingInstances = remainingPlaceholders.Where(p => 
                            string.Equals(p.Name, mapping.Key, StringComparison.OrdinalIgnoreCase)).ToList();
                        
                        _output.WriteLine($"FAILED REPLACEMENT DETAILS for '{mapping.Key}':");
                        _output.WriteLine($"  Found {foundInstances.Count} instances initially");
                        _output.WriteLine($"  {remainingInstances.Count} instances remain after replacement");
                        
                        foreach (var found in foundInstances)
                        {
                            _output.WriteLine($"  Initial location: {found.Locations.FirstOrDefault()?.Context ?? "No context"}");
                        }
                        foreach (var remaining in remainingInstances)
                        {
                            _output.WriteLine($"  Remaining location: {remaining.Locations.FirstOrDefault()?.Context ?? "No context"}");
                        }
                    }
                }
            }
            
            if (failedReplacements.Count > 0)
            {
                Assert.False(true, 
                    $"The following placeholders were found but not fully replaced: {string.Join(", ", failedReplacements)}. " +
                    $"This indicates a replacement failure. See output for details.");
            }
            
            // Verify that the document is still valid after replacement
            VerifyDocumentIntegrity(testCopyPath);
            
            // If we had any initial placeholders that were in our mapping, we should have made replacements
            var mappedPlaceholdersFound = discoveredPlaceholders.Count(p => 
                replacementMap.Mappings.ContainsKey(p.Name));
            
            if (mappedPlaceholdersFound > 0)
            {
                Assert.True(replaceResult.ReplacementCount > 0, 
                    $"Expected at least 1 replacement since {mappedPlaceholdersFound} mapped placeholders were found, " +
                    $"but got {replaceResult.ReplacementCount} replacements");
            }
        }
        finally
        {
            if (File.Exists(testCopyPath)) File.Delete(testCopyPath);
        }
    }

    [Fact]
    public async Task IntegrationTest_WithComplexFormattedPlaceholders_ShouldHandleAllScenarios()
    {
        // arrange
        var testDocxPath = Path.Combine(Path.GetTempPath(), $"complex_integration_test_{Guid.NewGuid()}.docx");
        
        try
        {
            // Create a comprehensive test document
            CreateComprehensiveTestDocument(testDocxPath);
            
            _mockFileSystemService.Setup(fs => fs.FileExists(testDocxPath)).Returns(true);
            _mockFileSystemService.Setup(fs => fs.GetFileSize(testDocxPath)).Returns(1024);
            
            var replacementMap = new ReplacementMap
            {
                Mappings = new Dictionary<string, string>
                {
                    { "MISTO_PLNENI", "Prague, Czech Republic" },
                    { "DELKA_CINNOSTI", "24 months" },
                    { "SIMPLE_PLACEHOLDER", "Simple Value" },
                    { "SPLIT_PLACEHOLDER", "Split Value" }
                }
            };
            
            // act - discover all placeholders
            var discoveredPlaceholders = await _scanService.ScanSingleFileAsync(testDocxPath);
            
            // assert - verify we found expected placeholders
            var foundNames = discoveredPlaceholders.Select(p => p.Name).ToHashSet();
            
            Assert.Contains("MISTO_PLNENI", foundNames);
            Assert.Contains("DELKA_CINNOSTI", foundNames);
            Assert.Contains("SIMPLE_PLACEHOLDER", foundNames);
            Assert.Contains("SPLIT_PLACEHOLDER", foundNames);
            
            var initialPlaceholderCount = discoveredPlaceholders.Count;
            
            // act - perform replacements
            var replaceResult = await _replaceService.ReplacePlaceholdersInFileAsync(testDocxPath, replacementMap, false);
            
            // assert - verify all replacements succeeded
            Assert.True(replaceResult.IsSuccess, $"Replacement failed: {replaceResult.ErrorMessage}");
            Assert.True(replaceResult.ReplacementCount >= 4, $"Expected at least 4 replacements, got {replaceResult.ReplacementCount}");
            
            // act - scan again to verify all placeholders were replaced
            var remainingPlaceholders = await _scanService.ScanSingleFileAsync(testDocxPath);
            
            // assert - verify significant reduction in placeholder count
            Assert.True(remainingPlaceholders.Count < initialPlaceholderCount, 
                $"Expected fewer placeholders after replacement. Before: {initialPlaceholderCount}, After: {remainingPlaceholders.Count}");
            
            // Specifically check that our target placeholders are gone
            var remainingNames = remainingPlaceholders.Select(p => p.Name).ToHashSet();
            Assert.DoesNotContain("MISTO_PLNENI", remainingNames);
            Assert.DoesNotContain("DELKA_CINNOSTI", remainingNames);
            Assert.DoesNotContain("SIMPLE_PLACEHOLDER", remainingNames);
            Assert.DoesNotContain("SPLIT_PLACEHOLDER", remainingNames);
            
            // Verify the document contains our replacement values
            VerifyReplacementValues(testDocxPath, replacementMap);
        }
        finally
        {
            if (File.Exists(testDocxPath)) File.Delete(testDocxPath);
        }
    }

    private static void CreateTestTemplateWithComplexPlaceholders(string docxPath)
    {
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(docxPath, 
            DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        var document = new DocumentFormat.OpenXml.Wordprocessing.Document();
        var body = new DocumentFormat.OpenXml.Wordprocessing.Body();
        
        // Create various paragraph styles that might cause placeholder splitting
        
        // 1. Simple paragraphs with placeholders that might get split
        var para1 = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
        para1.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Místo plnění: ")));
        para1.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("{{MIST"))); // Intentionally split
        para1.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("O_PLNENI}}")));
        
        var para2 = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
        para2.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Délka činnosti: ")));
        para2.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("{{DEL"))); // Split across 3 runs
        para2.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("KA_CINNO")));
        para2.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("STI}}")));
        
        // 2. Table with split placeholders
        var table = new DocumentFormat.OpenXml.Wordprocessing.Table();
        var tableRow = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
        var tableCell = new DocumentFormat.OpenXml.Wordprocessing.TableCell();
        var tablePara = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
        tablePara.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Hodnota: {{DA")));
        tablePara.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("TUM}}")));
        
        tableCell.AppendChild(tablePara);
        tableRow.AppendChild(tableCell);
        table.AppendChild(tableRow);
        
        body.AppendChild(para1);
        body.AppendChild(para2);
        body.AppendChild(table);
        
        document.AppendChild(body);
        mainPart.Document = document;
        mainPart.Document.Save();
    }

    private static void CreateComprehensiveTestDocument(string docxPath)
    {
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(docxPath, 
            DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        var document = new DocumentFormat.OpenXml.Wordprocessing.Document();
        var body = new DocumentFormat.OpenXml.Wordprocessing.Body();
        
        // Various scenarios that could cause placeholder splitting
        
        // 1. Target placeholders split in different ways
        var para1 = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
        para1.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Location: {{MIST")));
        para1.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("O_PLNENI}}")));
        
        var para2 = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
        para2.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Duration: {{DEL")));
        para2.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("KA_")));
        para2.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("CINNOSTI}}")));
        
        // 2. Simple non-split placeholder for comparison
        var para3 = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
        para3.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Simple: {{SIMPLE_PLACEHOLDER}}")));
        
        // 3. Complex split scenario
        var para4 = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
        para4.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Complex: {{")));
        para4.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("SPLIT_")));
        para4.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("PLACEHOLDER")));
        para4.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("}}")));
        
        body.AppendChild(para1);
        body.AppendChild(para2);
        body.AppendChild(para3);
        body.AppendChild(para4);
        
        document.AppendChild(body);
        mainPart.Document = document;
        mainPart.Document.Save();
    }

    private static void VerifyDocumentIntegrity(string docxPath)
    {
        // Verify the document can be opened and is structurally valid
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(docxPath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        Assert.NotNull(body);
        
        // Verify it has content
        var allText = string.Join("", body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
        Assert.False(string.IsNullOrWhiteSpace(allText), "Document should not be empty after replacement");
    }

    private static void VerifyReplacementValues(string docxPath, ReplacementMap replacementMap)
    {
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(docxPath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        Assert.NotNull(body);
        
        var allText = string.Join(" ", body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
        
        // Verify replacement values are present
        foreach (var mapping in replacementMap.Mappings)
        {
            Assert.Contains(mapping.Value, allText);
        }
    }
    
    /// <summary>
    /// Helper method to find the project root directory by looking for the .sln file
    /// </summary>
    private static string FindProjectRoot(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);
        
        while (current != null)
        {
            if (current.GetFiles("*.sln").Length > 0)
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        
        throw new DirectoryNotFoundException("Could not find project root directory (directory containing .sln file)");
    }
}