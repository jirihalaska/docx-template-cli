namespace DocxTemplate.Core.Services;

public interface IPlaceholderScanService
{
    /// <summary>
    /// Scans templates for placeholder patterns
    /// </summary>
    /// <param name="folderPath">Folder containing templates</param>
    /// <param name="pattern">Regex pattern for placeholders (default: {{.*?}})</param>
    /// <param name="recursive">Include subdirectories</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Discovered placeholders with their locations</returns>
    Task<PlaceholderScanResult> ScanPlaceholdersAsync(
        string folderPath,
        string pattern = @"\{\{.*?\}\}",
        bool recursive = true,
        CancellationToken cancellationToken = default);
}

public record PlaceholderScanResult
{
    public required IEnumerable<Placeholder> Placeholders { get; init; }
    public required int TotalFilesScanned { get; init; }
    public required TimeSpan ScanDuration { get; init; }
}

public record Placeholder
{
    public required string Name { get; init; }
    public required string Pattern { get; init; }
    public required IEnumerable<PlaceholderLocation> Locations { get; init; }
    public required int TotalOccurrences { get; init; }
}

public record PlaceholderLocation
{
    public required string FileName { get; init; }
    public required string FilePath { get; init; }
    public required int Occurrences { get; init; }
}