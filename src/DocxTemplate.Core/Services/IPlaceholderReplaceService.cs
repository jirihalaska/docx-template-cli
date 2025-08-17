namespace DocxTemplate.Core.Services;

public interface IPlaceholderReplaceService
{
    /// <summary>
    /// Replaces placeholders in templates with provided values
    /// </summary>
    /// <param name="folderPath">Folder containing templates</param>
    /// <param name="replacements">Map of placeholder to replacement value</param>
    /// <param name="createBackup">Create backup before replacing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Replacement operation result</returns>
    Task<ReplaceResult> ReplacePlaceholdersAsync(
        string folderPath,
        Dictionary<string, string> replacements,
        bool createBackup = true,
        CancellationToken cancellationToken = default);
}

public record ReplaceResult
{
    public required int FilesProcessed { get; init; }
    public required int TotalReplacements { get; init; }
    public required IEnumerable<FileReplaceResult> FileResults { get; init; }
    public required TimeSpan Duration { get; init; }
    public required bool HasErrors { get; init; }
}

public record FileReplaceResult
{
    public required string FilePath { get; init; }
    public required int ReplacementCount { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? BackupPath { get; init; }
}