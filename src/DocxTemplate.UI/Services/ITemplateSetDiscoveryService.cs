using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace DocxTemplate.UI.Services;

/// <summary>
/// Service for discovering template sets via CLI integration
/// </summary>
public interface ITemplateSetDiscoveryService
{
    /// <summary>
    /// Discovers template sets in the specified templates folder using CLI list-sets command
    /// </summary>
    /// <param name="templatesPath">Path to templates folder</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of template set information</returns>
    Task<IReadOnlyList<TemplateSetInfo>> DiscoverTemplateSetsAsync(
        string templatesPath, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a discovered template set
/// </summary>
public record TemplateSetInfo
{
    /// <summary>
    /// Name of the template set
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Full path to the template set directory
    /// </summary>
    public required string Path { get; init; }
    
    /// <summary>
    /// Number of templates in the set
    /// </summary>
    public required int FileCount { get; init; }
    
    /// <summary>
    /// Total size of all templates in bytes
    /// </summary>
    public required long TotalSize { get; init; }
    
    /// <summary>
    /// Formatted total size of the template set
    /// </summary>
    public required string TotalSizeFormatted { get; init; }
    
    /// <summary>
    /// Last modified date of the template set
    /// </summary>
    public required DateTime LastModified { get; init; }
}