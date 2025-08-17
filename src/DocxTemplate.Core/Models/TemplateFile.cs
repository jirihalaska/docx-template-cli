using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models;

/// <summary>
/// Represents a discovered template file with metadata
/// </summary>
public record TemplateFile
{
    /// <summary>
    /// Full absolute path to the template file
    /// </summary>
    [Required(ErrorMessage = "Template full path is required")]
    public required string FullPath { get; init; }

    /// <summary>
    /// Relative path from the discovery root directory
    /// </summary>
    [Required(ErrorMessage = "Template relative path is required")]
    public required string RelativePath { get; init; }

    /// <summary>
    /// Name of the template file including extension
    /// </summary>
    [Required(ErrorMessage = "Template file name is required")]
    public required string FileName { get; init; }

    /// <summary>
    /// Size of the template file in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "File size must be non-negative")]
    public required long SizeInBytes { get; init; }

    /// <summary>
    /// Last modification time of the template file
    /// </summary>
    public required DateTime LastModified { get; init; }

    /// <summary>
    /// Directory containing the template file
    /// </summary>
    public string DirectoryName => Path.GetDirectoryName(FullPath) ?? string.Empty;

    /// <summary>
    /// File name without extension
    /// </summary>
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(FileName);

    /// <summary>
    /// File extension (including the dot)
    /// </summary>
    public string Extension => Path.GetExtension(FileName);

    /// <summary>
    /// Gets a display-friendly size string (e.g., "2.3 MB")
    /// </summary>
    public string DisplaySize => FormatBytes(SizeInBytes);

    /// <summary>
    /// Validates that the template file exists and is a valid .docx file
    /// </summary>
    /// <returns>True if the template file is valid</returns>
    public bool IsValid()
    {
        // Check required fields
        if (string.IsNullOrWhiteSpace(FullPath) || 
            string.IsNullOrWhiteSpace(RelativePath) || 
            string.IsNullOrWhiteSpace(FileName))
            return false;

        // Check if file exists
        if (!File.Exists(FullPath))
            return false;

        // Check if file has .docx extension
        if (!Extension.Equals(".docx", StringComparison.OrdinalIgnoreCase))
            return false;

        // Check if relative path is properly formatted (no upward navigation)
        if (RelativePath.Contains(".."))
            return false;

        // Check if size is reasonable
        if (SizeInBytes < 0)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if the template file is accessible for reading
    /// </summary>
    /// <returns>True if the file can be read</returns>
    public bool IsAccessible()
    {
        try
        {
            using var stream = File.OpenRead(FullPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.#} {sizes[order]}";
    }
}