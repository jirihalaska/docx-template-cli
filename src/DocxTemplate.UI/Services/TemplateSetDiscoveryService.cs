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
    private readonly CliCommandBuilder _commandBuilder;
    private readonly CliResultParser _resultParser;

    public TemplateSetDiscoveryService(ICliCommandService cliCommandService, CliCommandBuilder commandBuilder)
    {
        _cliCommandService = cliCommandService ?? throw new ArgumentNullException(nameof(cliCommandService));
        _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
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
            // Use CliCommandBuilder to construct the command
            var cliCommand = _commandBuilder.BuildListSetsCommand(templatesPath);

            // Execute the command using the structured command data
            var jsonOutput = await _cliCommandService.ExecuteCommandAsync(
                cliCommand.CommandName, 
                cliCommand.Arguments);

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
                    Path = System.IO.Path.Combine(templatesPath, ts.Name ?? string.Empty),
                    FileCount = ts.FileCount,
                    TotalSize = ts.TotalSize,
                    TotalSizeFormatted = ts.TotalSizeFormatted ?? "0 B",
                    LastModified = ts.LastModified ?? DateTime.UtcNow
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
    public long TotalSize { get; init; }
    public string? TotalSizeFormatted { get; init; }
    public DateTime? LastModified { get; init; }
}