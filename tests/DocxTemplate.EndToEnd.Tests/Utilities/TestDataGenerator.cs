using DocxTemplate.TestUtilities;
using System.Text.Json;

namespace DocxTemplate.EndToEnd.Tests.Utilities;

/// <summary>
/// Generates test data for end-to-end testing scenarios
/// </summary>
public class TestDataGenerator
{
    private readonly TestDataManager _testDataManager;

    public TestDataGenerator()
    {
        _testDataManager = new TestDataManager();
    }

    /// <summary>
    /// Generates test documents for all complexity levels
    /// </summary>
    public async Task GenerateAllTestDataAsync(string baseDirectory)
    {
        // Simple documents
        await GenerateSimpleDocumentsAsync(Path.Combine(baseDirectory, "simple"));
        
        // Medium complexity documents
        await GenerateMediumDocumentsAsync(Path.Combine(baseDirectory, "medium"));
        
        // Complex documents
        await GenerateComplexDocumentsAsync(Path.Combine(baseDirectory, "complex"));
        
        // Create corresponding replacement mappings
        await GenerateReplacementMappingsAsync(baseDirectory);
    }

    /// <summary>
    /// Generates simple test documents with basic placeholders
    /// </summary>
    public async Task GenerateSimpleDocumentsAsync(string directory)
    {
        Directory.CreateDirectory(directory);

        // Simple contract template
        var contractPlaceholders = new List<string> { "client_name", "date", "amount" };
        await _testDataManager.CreateTestDocumentAsync(
            Path.Combine(directory, "SimpleContract.docx"), 
            contractPlaceholders);

        // Simple letter template
        var letterPlaceholders = new List<string> { "recipient", "sender", "subject" };
        await _testDataManager.CreateTestDocumentAsync(
            Path.Combine(directory, "SimpleLetter.docx"), 
            letterPlaceholders);

        // Simple invoice template
        var invoicePlaceholders = new List<string> { "invoice_number", "total", "due_date" };
        await _testDataManager.CreateTestDocumentAsync(
            Path.Combine(directory, "SimpleInvoice.docx"), 
            invoicePlaceholders);
    }

    /// <summary>
    /// Generates medium complexity documents with tables and formatting
    /// </summary>
    public async Task GenerateMediumDocumentsAsync(string directory)
    {
        Directory.CreateDirectory(directory);

        // Contract with table
        var contractSpec = new 
        {
            Name = "MediumContract",
            HasTables = true,
            HasImages = false,
            PlaceholderCount = 10,
            HasComplexFormatting = true,
            HasCzechCharacters = false
        };
        await _testDataManager.CreateComplexTestDocumentAsync(
            Path.Combine(directory, "ContractWithTable.docx"), 
            contractSpec);

        // Report with formatting
        var reportSpec = new 
        {
            Name = "FormattedReport",
            HasTables = true,
            HasImages = false,
            PlaceholderCount = 8,
            HasComplexFormatting = true,
            HasCzechCharacters = false
        };
        await _testDataManager.CreateComplexTestDocumentAsync(
            Path.Combine(directory, "FormattedReport.docx"), 
            reportSpec);

        // Czech document with basic formatting
        var czechSpec = new 
        {
            Name = "CzechBasic",
            HasTables = false,
            HasImages = false,
            PlaceholderCount = 6,
            HasComplexFormatting = false,
            HasCzechCharacters = true
        };
        await _testDataManager.CreateComplexTestDocumentAsync(
            Path.Combine(directory, "CzechDocument.docx"), 
            czechSpec);
    }

    /// <summary>
    /// Generates complex documents with all features
    /// </summary>
    public async Task GenerateComplexDocumentsAsync(string directory)
    {
        Directory.CreateDirectory(directory);

        // Full complex contract
        var fullContractSpec = new 
        {
            Name = "FullContract",
            HasTables = true,
            HasImages = true,
            PlaceholderCount = 20,
            HasComplexFormatting = true,
            HasCzechCharacters = false
        };
        await _testDataManager.CreateComplexTestDocumentAsync(
            Path.Combine(directory, "FullComplexContract.docx"), 
            fullContractSpec);

        // Czech heavy document
        var czechHeavySpec = new 
        {
            Name = "CzechHeavy",
            HasTables = true,
            HasImages = false,
            PlaceholderCount = 15,
            HasComplexFormatting = true,
            HasCzechCharacters = true
        };
        await _testDataManager.CreateComplexTestDocumentAsync(
            Path.Combine(directory, "CzechHeavyDocument.docx"), 
            czechHeavySpec);

        // Multi-section report
        var multiSectionSpec = new 
        {
            Name = "MultiSection",
            HasTables = true,
            HasImages = true,
            PlaceholderCount = 25,
            HasComplexFormatting = true,
            HasCzechCharacters = false
        };
        await _testDataManager.CreateComplexTestDocumentAsync(
            Path.Combine(directory, "MultiSectionReport.docx"), 
            multiSectionSpec);
    }

    /// <summary>
    /// Generates replacement mapping files for test documents
    /// </summary>
    public async Task GenerateReplacementMappingsAsync(string baseDirectory)
    {
        var mappingsDirectory = Path.Combine(baseDirectory, "mappings");
        Directory.CreateDirectory(mappingsDirectory);

        // Simple mappings
        var simpleMappings = new Dictionary<string, string>
        {
            { "client_name", "Simple Corp Ltd." },
            { "date", "2025-08-17" },
            { "amount", "$1,000.00" },
            { "recipient", "John Doe" },
            { "sender", "Jane Smith" },
            { "subject", "Test Subject" },
            { "invoice_number", "INV-001" },
            { "total", "$500.00" },
            { "due_date", "2025-09-17" }
        };

        await File.WriteAllTextAsync(
            Path.Combine(mappingsDirectory, "simple_mappings.json"),
            JsonSerializer.Serialize(simpleMappings, new JsonSerializerOptions { WriteIndented = true }));

        // Medium mappings
        var mediumMappings = new Dictionary<string, string>
        {
            { "placeholder_1", "Medium Value 1" },
            { "placeholder_2", "Medium Value 2" },
            { "placeholder_3", "Medium Value 3" },
            { "placeholder_4", "Medium Value 4" },
            { "placeholder_5", "Medium Value 5" },
            { "placeholder_6", "Medium Value 6" },
            { "placeholder_7", "Medium Value 7" },
            { "placeholder_8", "Medium Value 8" },
            { "placeholder_9", "Medium Value 9" },
            { "placeholder_10", "Medium Value 10" },
            { "table_header_1", "Column A" },
            { "table_header_2", "Column B" },
            { "table_data_1_1", "Row 1 Col A" },
            { "table_data_1_2", "Row 1 Col B" },
            { "table_data_2_1", "Row 2 Col A" },
            { "table_data_2_2", "Row 2 Col B" },
            { "table_data_3_1", "Row 3 Col A" },
            { "table_data_3_2", "Row 3 Col B" }
        };

        await File.WriteAllTextAsync(
            Path.Combine(mappingsDirectory, "medium_mappings.json"),
            JsonSerializer.Serialize(mediumMappings, new JsonSerializerOptions { WriteIndented = true }));

        // Complex mappings
        var complexMappings = new Dictionary<string, string>();
        for (int i = 1; i <= 25; i++)
        {
            complexMappings[$"placeholder_{i}"] = $"Complex Value {i}";
        }
        
        // Add table data
        for (int i = 1; i <= 3; i++)
        {
            complexMappings[$"table_data_{i}_1"] = $"Complex Row {i} Col A";
            complexMappings[$"table_data_{i}_2"] = $"Complex Row {i} Col B";
        }
        complexMappings["table_header_1"] = "Complex Column A";
        complexMappings["table_header_2"] = "Complex Column B";

        await File.WriteAllTextAsync(
            Path.Combine(mappingsDirectory, "complex_mappings.json"),
            JsonSerializer.Serialize(complexMappings, new JsonSerializerOptions { WriteIndented = true }));

        // Czech mappings
        var czechMappings = new Dictionary<string, string>
        {
            { "český_zástupce_1", "První česká hodnota" },
            { "český_zástupce_2", "Druhá česká hodnota" },
            { "český_zástupce_3", "Třetí česká hodnota" },
            { "český_zástupce_4", "Čtvrtá česká hodnota" },
            { "český_zástupce_5", "Pátá česká hodnota" },
            { "český_zástupce_6", "Šestá česká hodnota" },
            { "český_název", "Testovací dokument s českými znaky" },
            { "adresa_město", "Brno" },
            { "poznámka", "Poznámka s českými znaky: áčďéěíňóřšťúůýž ÁČĎÉĚÍŇÓŘŠŤÚŮÝŽ" }
        };

        for (int i = 7; i <= 15; i++)
        {
            czechMappings[$"český_zástupce_{i}"] = $"Česká hodnota číslo {i}";
        }

        await File.WriteAllTextAsync(
            Path.Combine(mappingsDirectory, "czech_mappings.json"),
            JsonSerializer.Serialize(czechMappings, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>
    /// Creates test template sets based on existing real templates
    /// </summary>
    public async Task CreateTestTemplateSetsAsync(string baseDirectory, string realTemplatesPath)
    {
        if (!Directory.Exists(realTemplatesPath))
        {
            // Fallback to generated templates if real ones don't exist
            await GenerateAllTestDataAsync(baseDirectory);
            return;
        }

        // Copy real template sets for testing
        var realTemplateSets = Directory.GetDirectories(realTemplatesPath);
        foreach (var realSet in realTemplateSets)
        {
            var setName = Path.GetFileName(realSet);
            var targetSetPath = Path.Combine(baseDirectory, "real-templates", setName);
            
            await CopyDirectoryAsync(realSet, targetSetPath);
        }
    }

    /// <summary>
    /// Generates large template sets for performance testing
    /// </summary>
    public async Task GenerateLargeTemplateSetsAsync(string directory, int documentCount = 50)
    {
        Directory.CreateDirectory(directory);

        var largeSetPath = Path.Combine(directory, "LargeTemplateSet");
        Directory.CreateDirectory(largeSetPath);

        // Generate multiple subdirectories with documents
        var categories = new[] { "Contracts", "Letters", "Reports", "Invoices", "Agreements" };
        
        foreach (var category in categories)
        {
            var categoryPath = Path.Combine(largeSetPath, category);
            Directory.CreateDirectory(categoryPath);

            // Generate documents in each category
            for (int i = 1; i <= documentCount / categories.Length; i++)
            {
                var placeholders = new List<string> 
                { 
                    $"{category.ToLower()}_field_1", 
                    $"{category.ToLower()}_field_2", 
                    $"{category.ToLower()}_field_3",
                    "common_field_1",
                    "common_field_2"
                };

                await _testDataManager.CreateTestDocumentAsync(
                    Path.Combine(categoryPath, $"{category}Template{i:D3}.docx"),
                    placeholders);
            }
        }
    }

    private async Task CopyDirectoryAsync(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(destDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile));
            File.Copy(file, destFile, true);
        }
    }
}