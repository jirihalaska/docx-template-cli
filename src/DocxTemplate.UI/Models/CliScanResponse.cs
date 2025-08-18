using System.Text.Json.Serialization;
using DocxTemplate.Core.Models;

namespace DocxTemplate.UI.Models;

/// <summary>
/// Represents the response structure from the CLI scan command
/// </summary>
public class CliScanResponse
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("data")]
    public CliScanData? Data { get; set; }
    
    /// <summary>
    /// Converts the CLI response to a PlaceholderScanResult
    /// </summary>
    public PlaceholderScanResult ToPlaceholderScanResult()
    {
        if (Data == null)
        {
            return PlaceholderScanResult.WithErrors(
                [],
                0,
                TimeSpan.Zero,
                0,
                1,
                [new ScanError { FilePath = "Unknown", Message = "CLI returned no data" }]
            );
        }

        var placeholders = Data.Placeholders.Select(cp => new Placeholder
        {
            Name = cp.Name,
            Pattern = cp.Pattern,
            TotalOccurrences = cp.TotalOccurrences,
            Locations = cp.Locations.Select(cl => new PlaceholderLocation
            {
                FilePath = cl.FilePath,
                FileName = cl.FileName,
                Occurrences = cl.Occurrences,
                Context = cl.Context
            }).ToList()
        }).ToList();

        var totalFilesScanned = placeholders.SelectMany(p => p.Locations).Select(l => l.FilePath).Distinct().Count();
        var filesWithPlaceholders = placeholders.Count(p => p.TotalOccurrences > 0);

        return Success 
            ? PlaceholderScanResult.Success(
                placeholders,
                totalFilesScanned,
                TimeSpan.FromMilliseconds(100), // CLI doesn't provide duration, use dummy value
                filesWithPlaceholders)
            : PlaceholderScanResult.WithErrors(
                placeholders,
                totalFilesScanned,
                TimeSpan.FromMilliseconds(100),
                filesWithPlaceholders,
                totalFilesScanned - filesWithPlaceholders,
                [new ScanError { FilePath = "Unknown", Message = "CLI scan failed" }]
            );
    }
}

/// <summary>
/// Represents the data section of the CLI scan response
/// </summary>
public class CliScanData
{
    [JsonPropertyName("placeholders")]
    public List<CliPlaceholder> Placeholders { get; set; } = [];
}

/// <summary>
/// Represents a placeholder from the CLI response
/// </summary>
public class CliPlaceholder
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;
    
    [JsonPropertyName("total_occurrences")]
    public int TotalOccurrences { get; set; }
    
    [JsonPropertyName("unique_files")]
    public int UniqueFiles { get; set; }
    
    [JsonPropertyName("locations")]
    public List<CliPlaceholderLocation> Locations { get; set; } = [];
}

/// <summary>
/// Represents a placeholder location from the CLI response
/// </summary>
public class CliPlaceholderLocation
{
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;
    
    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = string.Empty;
    
    [JsonPropertyName("occurrences")]
    public int Occurrences { get; set; }
    
    [JsonPropertyName("context")]
    public string Context { get; set; } = string.Empty;
}