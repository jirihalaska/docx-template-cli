namespace DocxTemplate.Core.Services;

public interface ITemplateCopyService
{
    /// <summary>
    /// Copies templates from source to target directory
    /// </summary>
    /// <param name="sourcePath">Source directory path</param>
    /// <param name="targetPath">Target directory path</param>
    /// <param name="preserveStructure">Maintain directory structure</param>
    /// <param name="overwrite">Overwrite existing files</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Copy operation result</returns>
    Task<CopyResult> CopyTemplatesAsync(
        string sourcePath,
        string targetPath,
        bool preserveStructure = true,
        bool overwrite = false,
        CancellationToken cancellationToken = default);
}

public record CopyResult
{
    public required int FilesCount { get; init; }
    public required long TotalBytesCount { get; init; }
    public required IEnumerable<CopiedFile> CopiedFiles { get; init; }
    public required TimeSpan Duration { get; init; }
}

public record CopiedFile
{
    public required string SourcePath { get; init; }
    public required string TargetPath { get; init; }
    public required long SizeInBytes { get; init; }
}