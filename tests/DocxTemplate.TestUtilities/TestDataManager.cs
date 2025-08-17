using DocxTemplate.Core.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocxTemplate.TestUtilities;

/// <summary>
/// Manages test data creation and cleanup for testing scenarios
/// </summary>
public static class TestDataManager
{
    /// <summary>
    /// Creates a temporary directory for test data
    /// </summary>
    public static string CreateTestDirectory(string testName)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "docx-template-tests", testName, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);
        return tempPath;
    }

    /// <summary>
    /// Cleans up test directory and all its contents
    /// </summary>
    public static void CleanupTestDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    /// <summary>
    /// Creates a Word document with specified placeholders
    /// </summary>
    public static void CreateTestDocumentWithPlaceholders(string filePath, Dictionary<string, string> placeholders)
    {
        using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
        
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        foreach (var placeholder in placeholders)
        {
            var paragraph = new Paragraph();
            var run = new Run();
            run.Append(new Text($"This document contains placeholder {placeholder.Key} with value {placeholder.Value}"));
            paragraph.Append(run);
            body.Append(paragraph);
        }

        mainPart.Document.Save();
    }

    /// <summary>
    /// Creates a standard test document with Czech characters
    /// </summary>
    public static void CreateCzechTestDocument(string filePath)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "{{název}}", "Testovací dokument" },
            { "{{město}}", "Brno" },
            { "{{ulice}}", "Údolní" },
            { "{{číslo}}", "53" }
        };

        CreateTestDocumentWithPlaceholders(filePath, placeholders);
    }

    /// <summary>
    /// Creates a complex test document with tables, headers, and footers
    /// </summary>
    public static void CreateComplexTestDocument(string filePath)
    {
        using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
        
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        // Add header with placeholder
        var headerPart = mainPart.AddNewPart<HeaderPart>();
        headerPart.Header = new Header();
        var headerParagraph = new Paragraph();
        headerParagraph.Append(new Run(new Text("Header with {{company_name}}")));
        headerPart.Header.Append(headerParagraph);

        // Add footer with placeholder
        var footerPart = mainPart.AddNewPart<FooterPart>();
        footerPart.Footer = new Footer();
        var footerParagraph = new Paragraph();
        footerParagraph.Append(new Run(new Text("Footer with {{date}} and {{page_number}}")));
        footerPart.Footer.Append(footerParagraph);

        // Add body content with table
        var paragraph = new Paragraph();
        paragraph.Append(new Run(new Text("Document body with {{client_name}}")));
        body.Append(paragraph);

        // Add table with placeholders
        var table = new Table();
        var row = new TableRow();
        var cell1 = new TableCell();
        cell1.Append(new Paragraph(new Run(new Text("{{product_name}}"))));
        var cell2 = new TableCell();
        cell2.Append(new Paragraph(new Run(new Text("{{product_price}}"))));
        row.Append(cell1, cell2);
        table.Append(row);
        body.Append(table);

        mainPart.Document.Save();
    }

    /// <summary>
    /// Creates test template files for template set testing
    /// </summary>
    public static void CreateTestTemplateSet(string baseDirectory, string setName)
    {
        var setPath = Path.Combine(baseDirectory, setName);
        Directory.CreateDirectory(setPath);

        // Create subdirectories
        var folders = new[] { "Contracts", "Letters", "Reports" };
        foreach (var folder in folders)
        {
            var folderPath = Path.Combine(setPath, folder);
            Directory.CreateDirectory(folderPath);

            // Create test documents in each folder
            for (int i = 1; i <= 3; i++)
            {
                var docPath = Path.Combine(folderPath, $"{folder}Template{i}.docx");
                CreateTestDocumentWithPlaceholders(docPath, new Dictionary<string, string>
                {
                    { "{{client}}", "Test Client" },
                    { "{{date}}", "2025-01-01" },
                    { "{{type}}", folder }
                });
            }
        }
    }

    /// <summary>
    /// Creates a corrupted Word document for error testing
    /// </summary>
    public static void CreateCorruptedDocument(string filePath)
    {
        // Create a file with invalid Word document structure
        File.WriteAllText(filePath, "This is not a valid Word document");
    }

    /// <summary>
    /// Creates replacement mapping for test scenarios
    /// </summary>
    public static ReplacementMap CreateTestReplacementMap()
    {
        return new ReplacementMap
        {
            Mappings = new Dictionary<string, string>
            {
                { "client", "Acme Corporation" },
                { "date", "2025-08-17" },
                { "company_name", "Test Company Ltd." },
                { "název", "Testovací dokument" },
                { "město", "Praha" },
                { "product_name", "Software License" },
                { "product_price", "$1,000.00" }
            }
        };
    }
}