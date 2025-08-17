namespace DocxTemplate.Core.Services;

public interface ITemplateDiscoveryService
{
    /// <summary>
    /// Discovers all .docx template files in the specified directory
    /// </summary>
    /// <param name="folderPath">Path to search for templates</param>
    /// <param name="recursive">Include subdirectories</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of discovered template files</returns>
    Task<IEnumerable<TemplateFile>> DiscoverTemplatesAsync(
        string folderPath, 
        bool recursive = true,
        CancellationToken cancellationToken = default);
}

public record TemplateFile
{
    public required string FullPath { get; init; }
    public required string RelativePath { get; init; }
    public required string FileName { get; init; }
    public required long SizeInBytes { get; init; }
    public required DateTime LastModified { get; init; }
}