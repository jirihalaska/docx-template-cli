using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace DocxTemplate.EndToEnd.Tests.Utilities;

/// <summary>
/// Validates Word document integrity and character preservation
/// </summary>
public class DocumentIntegrityValidator
{
    /// <summary>
    /// Validates that a document maintains its integrity after processing
    /// </summary>
    public async Task<DocumentValidationResult> ValidateDocumentIntegrityAsync(
        string originalPath,
        string processedPath)
    {
        if (!File.Exists(originalPath))
            throw new FileNotFoundException($"Original document not found: {originalPath}");

        if (!File.Exists(processedPath))
            throw new FileNotFoundException($"Processed document not found: {processedPath}");

        var result = new DocumentValidationResult
        {
            OriginalPath = originalPath,
            ProcessedPath = processedPath
        };

        try
        {
            // Validate basic file integrity
            result.IsValidDocxFormat = await ValidateDocxFormatAsync(processedPath);

            // Validate document structure
            var structureValidation = await ValidateDocumentStructureAsync(originalPath, processedPath);
            result.StructurePreserved = structureValidation.IsStructurePreserved;
            result.StructureIssues.AddRange(structureValidation.Issues);

            // Validate character preservation
            var characterValidation = await ValidateCharacterPreservationAsync(originalPath, processedPath);
            result.CharactersPreserved = characterValidation.IsCharactersPreserved;
            result.CharacterIssues.AddRange(characterValidation.Issues);

            result.IsValid = result.IsValidDocxFormat &&
                           result.StructurePreserved &&
                           result.CharactersPreserved;
        }
        catch (Exception ex)
        {
            result.ValidationError = ex.Message;
            result.IsValid = false;
        }

        return result;
    }

    /// <summary>
    /// Validates that Czech and Unicode characters are preserved throughout processing
    /// </summary>
    public async Task<CharacterPreservationResult> ValidateCharacterPreservationAsync(
        string originalPath,
        string processedPath)
    {
        var result = new CharacterPreservationResult();

        try
        {
            var originalText = await ExtractDocumentTextAsync(originalPath);
            var processedText = await ExtractDocumentTextAsync(processedPath);

            // Check for specific Czech characters
            var czechChars = new[] { 'á', 'č', 'ď', 'é', 'ě', 'í', 'ň', 'ó', 'ř', 'š', 'ť', 'ú', 'ů', 'ý', 'ž',
                                   'Á', 'Č', 'Ď', 'É', 'Ě', 'Í', 'Ň', 'Ó', 'Ř', 'Š', 'Ť', 'Ú', 'Ů', 'Ý', 'Ž' };

            foreach (var czechChar in czechChars)
            {
                var originalCount = originalText.Count(c => c == czechChar);
                var processedCount = processedText.Count(c => c == czechChar);

                if (originalCount != processedCount)
                {
                    result.Issues.Add($"Czech character '{czechChar}' count mismatch: original={originalCount}, processed={processedCount}");
                }
            }

            // Check for Unicode preservation
            for (int i = 0; i < Math.Min(originalText.Length, processedText.Length); i++)
            {
                if (originalText[i] != processedText[i] &&
                    !IsPlaceholderChange(originalText, processedText, i))
                {
                    var originalChar = originalText[i];
                    var processedChar = processedText[i];

                    if (char.GetUnicodeCategory(originalChar) != char.GetUnicodeCategory(processedChar))
                    {
                        result.Issues.Add($"Unicode character category change at position {i}: '{originalChar}' ({char.GetUnicodeCategory(originalChar)}) -> '{processedChar}' ({char.GetUnicodeCategory(processedChar)})");
                    }
                }
            }

            result.IsCharactersPreserved = result.Issues.Count == 0;
        }
        catch (Exception ex)
        {
            result.Issues.Add($"Character validation error: {ex.Message}");
            result.IsCharactersPreserved = false;
        }

        return result;
    }

    private Task<bool> ValidateDocxFormatAsync(string filePath)
    {
        try
        {
            using var document = WordprocessingDocument.Open(filePath, false);
            return Task.FromResult(document.MainDocumentPart?.Document != null);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private Task<DocumentStructureResult> ValidateDocumentStructureAsync(
        string originalPath,
        string processedPath)
    {
        var result = new DocumentStructureResult();

        try
        {
            using var originalDoc = WordprocessingDocument.Open(originalPath, false);
            using var processedDoc = WordprocessingDocument.Open(processedPath, false);

            // Validate main document parts exist
            if (originalDoc.MainDocumentPart == null || processedDoc.MainDocumentPart == null)
            {
                result.Issues.Add("Missing main document part");
                return Task.FromResult(result);
            }

            // Check paragraph count
            var originalParas = originalDoc.MainDocumentPart.Document.Body?.Elements<Paragraph>().Count() ?? 0;
            var processedParas = processedDoc.MainDocumentPart.Document.Body?.Elements<Paragraph>().Count() ?? 0;

            if (originalParas != processedParas)
            {
                result.Issues.Add($"Paragraph count mismatch: original={originalParas}, processed={processedParas}");
            }

            // Check table count
            var originalTables = originalDoc.MainDocumentPart.Document.Body?.Elements<Table>().Count() ?? 0;
            var processedTables = processedDoc.MainDocumentPart.Document.Body?.Elements<Table>().Count() ?? 0;

            if (originalTables != processedTables)
            {
                result.Issues.Add($"Table count mismatch: original={originalTables}, processed={processedTables}");
            }

            result.IsStructurePreserved = result.Issues.Count == 0;
        }
        catch (Exception ex)
        {
            result.Issues.Add($"Structure validation error: {ex.Message}");
        }

        return Task.FromResult(result);
    }

    private Task<string> ExtractDocumentTextAsync(string filePath)
    {
        using var document = WordprocessingDocument.Open(filePath, false);
        var body = document.MainDocumentPart?.Document?.Body;

        if (body == null)
            return Task.FromResult(string.Empty);

        var text = new StringBuilder();

        foreach (var element in body.Descendants<Text>())
        {
            text.Append(element.Text);
        }

        return Task.FromResult(text.ToString());
    }

    private bool IsPlaceholderChange(string original, string processed, int position)
    {
        // Simple heuristic: if we're inside {{ }} brackets, it might be a placeholder replacement
        var searchStart = Math.Max(0, position - 50);
        var searchEnd = Math.Min(original.Length, position + 50);

        var context = original.Substring(searchStart, searchEnd - searchStart);
        return context.Contains("{{") && context.Contains("}}");
    }
}

public class DocumentValidationResult
{
    public string OriginalPath { get; set; } = string.Empty;
    public string ProcessedPath { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public bool IsValidDocxFormat { get; set; }
    public bool StructurePreserved { get; set; }
    public bool CharactersPreserved { get; set; }
    public List<string> StructureIssues { get; set; } = [];
    public List<string> CharacterIssues { get; set; } = [];
    public string ValidationError { get; set; } = string.Empty;
}

public class CharacterPreservationResult
{
    public bool IsCharactersPreserved { get; set; }
    public List<string> Issues { get; set; } = [];
}

public class DocumentStructureResult
{
    public bool IsStructurePreserved { get; set; }
    public List<string> Issues { get; set; } = [];
}
