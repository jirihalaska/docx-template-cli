using DocxTemplate.Core.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxTemplate.Processing.Models;

namespace DocxTemplate.TestUtilities;

/// <summary>
/// Manages test data creation and cleanup for testing scenarios
/// </summary>
public class TestDataManager
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

    /// <summary>
    /// Creates a Word document with specified placeholders asynchronously
    /// </summary>
    public async Task CreateTestDocumentAsync(string filePath, IList<string> placeholders, bool includeCzechCharacters = false)
    {
        using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);

        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        // Add title
        var titleParagraph = new Paragraph();
        var titleRun = new Run();
        var titleText = includeCzechCharacters ? "Testovací dokument s českými znaky" : "Test Document";
        titleRun.Append(new Text(titleText));
        titleParagraph.Append(titleRun);
        body.Append(titleParagraph);

        // Add placeholders
        foreach (var placeholder in placeholders)
        {
            var paragraph = new Paragraph();
            var run = new Run();
            var text = includeCzechCharacters ?
                $"Tento dokument obsahuje zástupný symbol {{{{{placeholder}}}}} pro testování." :
                $"This document contains placeholder {{{{{placeholder}}}}} for testing.";
            run.Append(new Text(text));
            paragraph.Append(run);
            body.Append(paragraph);
        }

        if (includeCzechCharacters)
        {
            // Add specific Czech characters test content
            var czechParagraph = new Paragraph();
            var czechRun = new Run();
            czechRun.Append(new Text("Specifické české znaky: áčďéěíňóřšťúůýž ÁČĎÉĚÍŇÓŘŠŤÚŮÝŽ"));
            czechParagraph.Append(czechRun);
            body.Append(czechParagraph);
        }

        mainPart.Document.Save();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates a complex test document with various elements asynchronously
    /// </summary>
    public async Task CreateComplexTestDocumentAsync(string filePath, dynamic complexitySpec)
    {
        using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);

        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        // Add title
        var titleParagraph = new Paragraph();
        var titleRun = new Run();
        titleRun.Append(new Text($"Complex Test Document - {complexitySpec.Name}"));
        titleParagraph.Append(titleRun);
        body.Append(titleParagraph);

        // Add placeholders based on complexity
        for (int i = 1; i <= complexitySpec.PlaceholderCount; i++)
        {
            var paragraph = new Paragraph();
            var run = new Run();
            var placeholderName = complexitySpec.HasCzechCharacters ? $"český_zástupce_{i}" : $"placeholder_{i}";
            run.Append(new Text($"Placeholder {{{{{placeholderName}}}}} in document."));
            paragraph.Append(run);
            body.Append(paragraph);
        }

        // Add table if specified
        if (complexitySpec.HasTables)
        {
            var table = new Table();

            // Add table header
            var headerRow = new TableRow();
            var headerCell1 = new TableCell();
            headerCell1.Append(new Paragraph(new Run(new Text("Column 1 - {{table_header_1}}"))));
            var headerCell2 = new TableCell();
            headerCell2.Append(new Paragraph(new Run(new Text("Column 2 - {{table_header_2}}"))));
            headerRow.Append(headerCell1, headerCell2);
            table.Append(headerRow);

            // Add data rows
            for (int i = 1; i <= 3; i++)
            {
                var row = new TableRow();
                var cell1 = new TableCell();
                cell1.Append(new Paragraph(new Run(new Text($"{{{{table_data_{i}_1}}}}"))));
                var cell2 = new TableCell();
                cell2.Append(new Paragraph(new Run(new Text($"{{{{table_data_{i}_2}}}}"))));
                row.Append(cell1, cell2);
                table.Append(row);
            }

            body.Append(table);
        }

        // Add Czech characters if specified
        if (complexitySpec.HasCzechCharacters)
        {
            var czechParagraph = new Paragraph();
            var czechRun = new Run();
            czechRun.Append(new Text("Testování českých znaků: {{český_název}}, {{adresa_město}}, {{poznámka}}"));
            czechParagraph.Append(czechRun);
            body.Append(czechParagraph);
        }

        mainPart.Document.Save();
        await Task.CompletedTask;
    }
}
