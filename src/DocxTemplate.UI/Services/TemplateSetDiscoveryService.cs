using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DocxTemplate.UI.Services;

/// <summary>
/// Implementation of template set discovery using CLI list-sets command
/// </summary>
public class TemplateSetDiscoveryService : ITemplateSetDiscoveryService
{
    private readonly ICliCommandService _cliCommandService;
    private readonly CliResultParser _resultParser;

    public TemplateSetDiscoveryService(ICliCommandService cliCommandService)
    {
        _cliCommandService = cliCommandService ?? throw new ArgumentNullException(nameof(cliCommandService));
        _resultParser = new CliResultParser();
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
            var arguments = new[]
            {
                "--templates", templatesPath,
                "--format", "json"
            };

            var jsonOutput = await _cliCommandService.ExecuteCommandAsync("list-sets", arguments);

            if (string.IsNullOrWhiteSpace(jsonOutput))
            {
                return Array.Empty<TemplateSetInfo>();
            }

            if (!_resultParser.IsSuccessResponse(jsonOutput))
            {
                throw new InvalidOperationException("CLI command returned unsuccessful response");
            }

            var response = _resultParser.ParseJsonResult<ListSetsResponse>(jsonOutput);
            
            return response.Data?.TemplateSets?
                .Select(ts => new TemplateSetInfo
                {
                    Name = ts.Name ?? string.Empty,
                    FileCount = ts.FileCount,
                    TotalSizeFormatted = ts.TotalSizeFormatted ?? "0 B"
                })
                .ToArray() ?? Array.Empty<TemplateSetInfo>();
        }
        catch (InvalidOperationException)
        {
            // CLI command failed - return empty result instead of throwing
            // This allows the UI to show appropriate error messages
            return Array.Empty<TemplateSetInfo>();
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            // Other unexpected errors - return empty result
            return Array.Empty<TemplateSetInfo>();
        }
    }
}

/// <summary>
/// Response model for CLI list-sets command
/// </summary>
internal record ListSetsResponse
{
    public string? Command { get; init; }
    public bool Success { get; init; }
    public ListSetsData? Data { get; init; }
}

/// <summary>
/// Data section of CLI list-sets response
/// </summary>
internal record ListSetsData
{
    public IReadOnlyList<TemplateSetResponse>? TemplateSets { get; init; }
}

/// <summary>
/// Individual template set in CLI response
/// </summary>
internal record TemplateSetResponse
{
    public string? Name { get; init; }
    public int FileCount { get; init; }
    public string? TotalSizeFormatted { get; init; }
}