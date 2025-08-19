using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocxTemplate.Core.Services;

namespace DocxTemplate.UI.Services;

/// <summary>
/// Implementation of template set discovery using Infrastructure services directly
/// </summary>
public class TemplateSetDiscoveryService : ITemplateSetDiscoveryService
{
    private readonly ITemplateSetService _templateSetService;

    public TemplateSetDiscoveryService(ITemplateSetService templateSetService)
    {
        _templateSetService = templateSetService ?? throw new ArgumentNullException(nameof(templateSetService));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateSetInfo>> DiscoverTemplateSetsAsync(
        string templatesPath, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templatesPath))
            throw new ArgumentException("Templates path cannot be null or empty.", nameof(templatesPath));

        try
        {
            var templateSets = await _templateSetService.ListTemplateSetsAsync(
                templatesPath, 
                includeEmptyFolders: false, 
                cancellationToken);

            return templateSets
                .Select(ts => new TemplateSetInfo
                {
                    Name = ts.Name,
                    Path = ts.FullPath,
                    FileCount = ts.TemplateCount,
                    TotalSize = ts.TotalSizeBytes,
                    TotalSizeFormatted = FormatBytes(ts.TotalSizeBytes),
                    LastModified = ts.LastModified
                })
                .ToArray();
        }
        catch (Exception)
        {
            // Return empty result on error instead of throwing
            // This allows the UI to show appropriate error messages
            return Array.Empty<TemplateSetInfo>();
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}