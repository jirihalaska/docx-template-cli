using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models;

/// <summary>
/// Represents a Word document template with metadata and validation
/// </summary>
public record Template
{
    /// <summary>
    /// File name of the template
    /// </summary>
    [Required(ErrorMessage = "Template file name is required")]
    public required string FileName { get; init; }

    /// <summary>
    /// Relative path from the template set root
    /// </summary>
    [Required(ErrorMessage = "Template relative path is required")]
    public required string RelativePath { get; init; }

    /// <summary>
    /// Full absolute path to the template file
    /// </summary>
    [Required(ErrorMessage = "Template full path is required")]
    public required string FullPath { get; init; }

    /// <summary>
    /// Size of the template file in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "File size must be non-negative")]
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Last modification time of the template file
    /// </summary>
    public required DateTime LastModified { get; init; }

    /// <summary>
    /// Validates that the template file exists and is a valid .docx file
    /// </summary>
    /// <returns>True if the template is valid</returns>
    public bool IsValid()
    {
        // Check if file exists
        if (!File.Exists(FullPath))
            return false;

        // Check if file has .docx extension
        if (!Path.GetExtension(FullPath).Equals(".docx", StringComparison.OrdinalIgnoreCase))
            return false;

        // Check if relative path is properly formatted
        if (string.IsNullOrWhiteSpace(RelativePath) || RelativePath.Contains(".."))
            return false;

        return true;
    }

    /// <summary>
    /// Gets a display-friendly size string (e.g., "2.3 MB")
    /// </summary>
    public string DisplaySize => FormatBytes(SizeBytes);

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = ["B", "KB", "MB", "GB"];
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} {sizes[order]}";
    }
}