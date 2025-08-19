using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DocxTemplate.UI.Tests.Helpers;

/// <summary>
/// Helper class for validating DOCX document content in tests
/// </summary>
public static class DocumentContentValidator
{
    /// <summary>
    /// Scans a document for placeholders using the placeholder scan service
    /// </summary>
    /// <param name="filePath">Path to the DOCX file</param>
    /// <param name="serviceProvider">Service provider for getting scanner</param>
    /// <param name="pattern">Placeholder pattern to search for</param>
    /// <returns>List of placeholders found in the document</returns>
    public static async Task<IReadOnlyList<Placeholder>> ScanDocumentAsync(
        string filePath, 
        IServiceProvider serviceProvider,
        string pattern = @"\{\{.*?\}\}")
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Document file not found: {filePath}");
        }

        var scanService = serviceProvider.GetRequiredService<IPlaceholderScanService>();
        return await scanService.ScanSingleFileAsync(filePath, pattern);
    }

    /// <summary>
    /// Validates that a document contains no remaining placeholders
    /// </summary>
    /// <param name="filePath">Path to the DOCX file</param>
    /// <param name="serviceProvider">Service provider for getting scanner</param>
    /// <param name="pattern">Placeholder pattern to search for</param>
    /// <returns>True if no placeholders found</returns>
    public static async Task<bool> ValidateNoPlaceholders(
        string filePath,
        IServiceProvider serviceProvider,
        string pattern = @"\{\{.*?\}\}")
    {
        var result = await ScanDocumentAsync(filePath, serviceProvider, pattern);
        return result.Count == 0;
    }

    /// <summary>
    /// Validates that a document contains expected Czech characters in the content
    /// </summary>
    /// <param name="filePath">Path to the DOCX file</param>
    /// <param name="expectedContent">Dictionary of expected content strings</param>
    /// <returns>True if all expected content is found</returns>
    public static async Task<bool> ValidateCzechCharacters(string filePath, Dictionary<string, string> expectedContent)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Document file not found: {filePath}");
        }

        try
        {
            var documentText = await ExtractDocumentText(filePath);
            
            // Check for each expected content string
            foreach (var kvp in expectedContent)
            {
                if (!documentText.Contains(kvp.Value, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            
            return true;
        }
        catch (Exception)
        {
            // If we can't extract text, assume validation failed
            return false;
        }
    }

    /// <summary>
    /// Validates that specific text exists in the document
    /// </summary>
    /// <param name="filePath">Path to the DOCX file</param>
    /// <param name="expectedText">Text that should be present</param>
    /// <param name="ignoreCase">Whether to ignore case in comparison</param>
    /// <returns>True if text is found</returns>
    public static async Task<bool> ValidateTextExists(string filePath, string expectedText, bool ignoreCase = true)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Document file not found: {filePath}");
        }

        try
        {
            var documentText = await ExtractDocumentText(filePath);
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return documentText.Contains(expectedText, comparison);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a list of all placeholders remaining in a document
    /// </summary>
    /// <param name="filePath">Path to the DOCX file</param>
    /// <param name="serviceProvider">Service provider for getting scanner</param>
    /// <param name="pattern">Placeholder pattern to search for</param>
    /// <returns>List of remaining placeholder names</returns>
    public static async Task<IReadOnlyList<string>> GetRemainingPlaceholders(
        string filePath,
        IServiceProvider serviceProvider,
        string pattern = @"\{\{.*?\}\}")
    {
        var result = await ScanDocumentAsync(filePath, serviceProvider, pattern);
        return result.Select(p => p.Name).ToList();
    }

    /// <summary>
    /// Validates that the document file size is reasonable
    /// </summary>
    /// <param name="filePath">Path to the DOCX file</param>
    /// <param name="minSizeBytes">Minimum expected file size</param>
    /// <param name="maxSizeBytes">Maximum expected file size</param>
    /// <returns>True if file size is within expected range</returns>
    public static bool ValidateFileSize(string filePath, long minSizeBytes = 1000, long maxSizeBytes = 10_000_000)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        var fileInfo = new FileInfo(filePath);
        return fileInfo.Length >= minSizeBytes && fileInfo.Length <= maxSizeBytes;
    }

    /// <summary>
    /// Validates that the document is a valid DOCX file
    /// </summary>
    /// <param name="filePath">Path to the DOCX file</param>
    /// <returns>True if file appears to be a valid DOCX</returns>
    public static bool ValidateDocxFormat(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            // DOCX files are ZIP archives, check for ZIP signature
            using var stream = File.OpenRead(filePath);
            var buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            
            // ZIP file signature: 0x50, 0x4B, 0x03, 0x04
            return buffer[0] == 0x50 && buffer[1] == 0x4B && 
                   (buffer[2] == 0x03 || buffer[2] == 0x05 || buffer[2] == 0x07) && 
                   (buffer[3] == 0x04 || buffer[3] == 0x06 || buffer[3] == 0x08);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a validation summary for a processed document
    /// </summary>
    /// <param name="filePath">Path to the DOCX file</param>
    /// <param name="serviceProvider">Service provider for services</param>
    /// <param name="expectedReplacements">Expected replacement values</param>
    /// <returns>Validation summary</returns>
    public static async Task<DocumentValidationSummary> CreateValidationSummary(
        string filePath,
        IServiceProvider serviceProvider,
        Dictionary<string, string>? expectedReplacements = null)
    {
        var summary = new DocumentValidationSummary
        {
            FilePath = filePath,
            FileExists = File.Exists(filePath),
            IsValidDocx = ValidateDocxFormat(filePath),
            FileSizeBytes = File.Exists(filePath) ? new FileInfo(filePath).Length : 0
        };

        if (summary.FileExists && summary.IsValidDocx)
        {
            try
            {
                var scanResult = await ScanDocumentAsync(filePath, serviceProvider);
                summary.RemainingPlaceholderCount = scanResult.Count;
                summary.RemainingPlaceholders = scanResult.Select(p => p.Name).ToList();
                
                if (expectedReplacements != null)
                {
                    summary.ExpectedContentValidation = await ValidateCzechCharacters(filePath, expectedReplacements);
                }
            }
            catch (Exception ex)
            {
                summary.ValidationError = ex.Message;
            }
        }

        return summary;
    }

    /// <summary>
    /// Extracts text content from a DOCX file for validation
    /// Note: This is a simplified extraction for testing purposes
    /// </summary>
    /// <param name="filePath">Path to the DOCX file</param>
    /// <returns>Extracted text content</returns>
    private static async Task<string> ExtractDocumentText(string filePath)
    {
        // This is a simplified text extraction for testing
        // In a real implementation, you might use DocumentFormat.OpenXml
        // For now, we'll use a basic approach
        
        try
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);
            var documentEntry = archive.GetEntry("word/document.xml");
            
            if (documentEntry == null)
            {
                return string.Empty;
            }
            
            using var stream = documentEntry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var xmlContent = await reader.ReadToEndAsync();
            
            // Simple regex to extract text between XML tags
            // This is very basic and may not capture all text accurately
            var textPattern = @"<w:t[^>]*>([^<]+)</w:t>";
            var matches = Regex.Matches(xmlContent, textPattern);
            
            var extractedText = new StringBuilder();
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    extractedText.Append(match.Groups[1].Value).Append(" ");
                }
            }
            
            return extractedText.ToString();
        }
        catch (Exception)
        {
            // If extraction fails, return empty string
            return string.Empty;
        }
    }
}

/// <summary>
/// Summary of document validation results
/// </summary>
public class DocumentValidationSummary
{
    /// <summary>
    /// Path to the validated document
    /// </summary>
    public required string FilePath { get; set; }
    
    /// <summary>
    /// Whether the file exists
    /// </summary>
    public bool FileExists { get; set; }
    
    /// <summary>
    /// Whether the file is a valid DOCX format
    /// </summary>
    public bool IsValidDocx { get; set; }
    
    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }
    
    /// <summary>
    /// Number of remaining placeholders in the document
    /// </summary>
    public int RemainingPlaceholderCount { get; set; }
    
    /// <summary>
    /// List of remaining placeholder names
    /// </summary>
    public List<string> RemainingPlaceholders { get; set; } = new();
    
    /// <summary>
    /// Whether expected content validation passed
    /// </summary>
    public bool? ExpectedContentValidation { get; set; }
    
    /// <summary>
    /// Any validation error that occurred
    /// </summary>
    public string? ValidationError { get; set; }
    
    /// <summary>
    /// Whether the document passed all validations
    /// </summary>
    public bool IsValid => FileExists && IsValidDocx && RemainingPlaceholderCount == 0 && 
                           ValidationError == null && (ExpectedContentValidation ?? true);
                           
    /// <summary>
    /// Gets a summary string of the validation results
    /// </summary>
    public override string ToString()
    {
        var fileName = Path.GetFileName(FilePath);
        var status = IsValid ? "VALID" : "INVALID";
        var details = new List<string>();
        
        if (!FileExists) details.Add("File missing");
        if (!IsValidDocx) details.Add("Invalid DOCX format");
        if (RemainingPlaceholderCount > 0) details.Add($"{RemainingPlaceholderCount} placeholders remaining");
        if (ExpectedContentValidation == false) details.Add("Expected content validation failed");
        if (ValidationError != null) details.Add($"Error: {ValidationError}");
        
        var detailsStr = details.Any() ? $" ({string.Join(", ", details)})" : "";
        return $"{fileName}: {status}{detailsStr}";
    }
}