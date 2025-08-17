using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models.Results;

/// <summary>
/// Estimate of disk space requirements for a copy operation
/// </summary>
public record CopySpaceEstimate
{
    /// <summary>
    /// Total size of files to be copied in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Total file size must be non-negative")]
    public required long TotalFileSizeBytes { get; init; }

    /// <summary>
    /// Estimated additional space needed for directory structure in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Directory overhead must be non-negative")]
    public required long DirectoryOverheadBytes { get; init; }

    /// <summary>
    /// Estimated space for file system metadata in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Metadata overhead must be non-negative")]
    public required long MetadataOverheadBytes { get; init; }

    /// <summary>
    /// Safety buffer percentage (0.0 to 1.0, e.g., 0.1 for 10%)
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Safety buffer must be between 0.0 and 1.0")]
    public required double SafetyBufferPercentage { get; init; }

    /// <summary>
    /// Number of files included in the estimate
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "File count must be non-negative")]
    public required int FileCount { get; init; }

    /// <summary>
    /// Number of directories that would be created
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Directory count must be non-negative")]
    public required int DirectoryCount { get; init; }

    /// <summary>
    /// Target file system type (affects overhead calculations)
    /// </summary>
    [Required(ErrorMessage = "File system type is required")]
    public required string FileSystemType { get; init; }

    /// <summary>
    /// Cluster size of the target file system in bytes
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Cluster size must be positive")]
    public required int ClusterSizeBytes { get; init; }

    /// <summary>
    /// Current available space on target drive in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Available space must be non-negative")]
    public long AvailableSpaceBytes { get; init; }

    /// <summary>
    /// Breakdown of space usage by file size categories
    /// </summary>
    [Required(ErrorMessage = "Size breakdown is required")]
    public required IReadOnlyDictionary<FileSizeCategory, long> SizeBreakdown { get; init; }

    /// <summary>
    /// Safety buffer size in bytes
    /// </summary>
    public long SafetyBufferBytes => (long)(TotalFileSizeBytes * SafetyBufferPercentage);

    /// <summary>
    /// Total estimated space needed including all overhead and safety buffer
    /// </summary>
    public long TotalEstimatedBytes => 
        TotalFileSizeBytes + DirectoryOverheadBytes + MetadataOverheadBytes + SafetyBufferBytes + ClusterWasteBytes;

    /// <summary>
    /// Estimated space wasted due to cluster size alignment
    /// </summary>
    public long ClusterWasteBytes
    {
        get
        {
            // Estimate average waste per file (half cluster size)
            return FileCount * (ClusterSizeBytes / 2);
        }
    }

    /// <summary>
    /// Indicates whether there's sufficient space available
    /// </summary>
    public bool HasSufficientSpace => AvailableSpaceBytes >= TotalEstimatedBytes;

    /// <summary>
    /// Remaining space after the copy operation (can be negative)
    /// </summary>
    public long RemainingSpaceBytes => AvailableSpaceBytes - TotalEstimatedBytes;

    /// <summary>
    /// Percentage of available space that would be used
    /// </summary>
    public double SpaceUsagePercentage => 
        AvailableSpaceBytes > 0 ? (TotalEstimatedBytes * 100.0) / AvailableSpaceBytes : 0;

    /// <summary>
    /// Average file size in bytes
    /// </summary>
    public double AverageFileSizeBytes => 
        FileCount > 0 ? (double)TotalFileSizeBytes / FileCount : 0;

    /// <summary>
    /// Gets a display-friendly total file size string
    /// </summary>
    public string DisplayTotalFileSize => FormatBytes(TotalFileSizeBytes);

    /// <summary>
    /// Gets a display-friendly total estimated size string
    /// </summary>
    public string DisplayTotalEstimated => FormatBytes(TotalEstimatedBytes);

    /// <summary>
    /// Gets a display-friendly available space string
    /// </summary>
    public string DisplayAvailableSpace => FormatBytes(AvailableSpaceBytes);

    /// <summary>
    /// Gets a display-friendly remaining space string
    /// </summary>
    public string DisplayRemainingSpace => FormatBytes(Math.Max(0, RemainingSpaceBytes));

    /// <summary>
    /// Gets a summary of the space estimate
    /// </summary>
    public string GetSummary()
    {
        return $"Space estimate: {DisplayTotalEstimated} needed for {FileCount} files ({DisplayTotalFileSize}). " +
               $"Available: {DisplayAvailableSpace}. " +
               $"Usage: {SpaceUsagePercentage:F1}%. " +
               (HasSufficientSpace ? 
                   $"Sufficient space available, {DisplayRemainingSpace} remaining." : 
                   $"Insufficient space, {FormatBytes(-RemainingSpaceBytes)} over limit.");
    }

    /// <summary>
    /// Gets a detailed breakdown of the estimate
    /// </summary>
    public string GetDetailedBreakdown()
    {
        var breakdown = new List<string>
        {
            $"File content: {FormatBytes(TotalFileSizeBytes)} ({FileCount} files)",
            $"Directory overhead: {FormatBytes(DirectoryOverheadBytes)} ({DirectoryCount} directories)",
            $"Metadata overhead: {FormatBytes(MetadataOverheadBytes)}",
            $"Cluster waste: {FormatBytes(ClusterWasteBytes)} (avg {ClusterSizeBytes} bytes/cluster)",
            $"Safety buffer: {FormatBytes(SafetyBufferBytes)} ({SafetyBufferPercentage:P0})",
            $"Total estimated: {DisplayTotalEstimated}",
            $"Available space: {DisplayAvailableSpace}",
            $"File system: {FileSystemType}"
        };

        return string.Join("\n", breakdown);
    }

    /// <summary>
    /// Creates a space estimate
    /// </summary>
    /// <param name="templateFiles">Template files to estimate for</param>
    /// <param name="targetPath">Target path for the copy</param>
    /// <param name="preserveStructure">Whether directory structure will be preserved</param>
    /// <param name="fileSystemType">Target file system type</param>
    /// <param name="clusterSize">Cluster size of target file system</param>
    /// <param name="availableSpace">Available space on target drive</param>
    /// <param name="safetyBufferPercentage">Safety buffer percentage</param>
    /// <returns>CopySpaceEstimate instance</returns>
    public static CopySpaceEstimate Create(
        IReadOnlyList<TemplateFile> templateFiles,
        string targetPath,
        bool preserveStructure,
        string fileSystemType = "NTFS",
        int clusterSize = 4096,
        long availableSpace = 0,
        double safetyBufferPercentage = 0.1)
    {
        var totalFileSize = templateFiles.Sum(f => f.SizeInBytes);
        var directoryCount = preserveStructure ? 
            templateFiles.Select(f => Path.GetDirectoryName(f.RelativePath)).Distinct().Count() : 1;

        // Estimate directory overhead (NTFS: ~4KB per directory)
        var directoryOverhead = directoryCount * 4096L;

        // Estimate metadata overhead (NTFS: ~1KB per file for MFT entries)
        var metadataOverhead = templateFiles.Count * 1024L;

        // Create size breakdown
        var sizeBreakdown = new Dictionary<FileSizeCategory, long>();
        foreach (var category in Enum.GetValues<FileSizeCategory>())
        {
            sizeBreakdown[category] = templateFiles
                .Where(f => GetFileSizeCategory(f.SizeInBytes) == category)
                .Sum(f => f.SizeInBytes);
        }

        return new CopySpaceEstimate
        {
            TotalFileSizeBytes = totalFileSize,
            DirectoryOverheadBytes = directoryOverhead,
            MetadataOverheadBytes = metadataOverhead,
            SafetyBufferPercentage = safetyBufferPercentage,
            FileCount = templateFiles.Count,
            DirectoryCount = directoryCount,
            FileSystemType = fileSystemType,
            ClusterSizeBytes = clusterSize,
            AvailableSpaceBytes = availableSpace,
            SizeBreakdown = sizeBreakdown
        };
    }

    private static FileSizeCategory GetFileSizeCategory(long sizeBytes)
    {
        return sizeBytes switch
        {
            < 1024 => FileSizeCategory.Tiny,
            < 10 * 1024 => FileSizeCategory.Small,
            < 100 * 1024 => FileSizeCategory.Medium,
            < 1024 * 1024 => FileSizeCategory.Large,
            < 10 * 1024 * 1024 => FileSizeCategory.VeryLarge,
            _ => FileSizeCategory.Huge
        };
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = Math.Abs(bytes);
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        var formatted = $"{size:0.#} {sizes[order]}";
        return bytes < 0 ? $"-{formatted}" : formatted;
    }
}

/// <summary>
/// Categories for file sizes in space estimation
/// </summary>
public enum FileSizeCategory
{
    /// <summary>
    /// Files smaller than 1 KB
    /// </summary>
    Tiny,

    /// <summary>
    /// Files between 1 KB and 10 KB
    /// </summary>
    Small,

    /// <summary>
    /// Files between 10 KB and 100 KB
    /// </summary>
    Medium,

    /// <summary>
    /// Files between 100 KB and 1 MB
    /// </summary>
    Large,

    /// <summary>
    /// Files between 1 MB and 10 MB
    /// </summary>
    VeryLarge,

    /// <summary>
    /// Files larger than 10 MB
    /// </summary>
    Huge
}