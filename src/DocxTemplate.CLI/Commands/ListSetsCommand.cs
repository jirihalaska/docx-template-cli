using System.CommandLine;
using System.Text.Json;
using DocxTemplate.Core.Services;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.CLI.Commands;

/// <summary>
/// Command to list all template sets in a templates directory
/// </summary>
public class ListSetsCommand : Command
{
    public ListSetsCommand() : base("list-sets", "List all template sets in the templates directory")
    {
        var templatesOption = new Option<string>(
            new[] { "--templates", "-t" },
            "Path to the templates root directory")
        {
            IsRequired = true
        };

        var formatOption = new Option<OutputFormat>(
            new[] { "--format", "-f" },
            getDefaultValue: () => OutputFormat.Text,
            "Output format (text, json, table, list)");

        var detailsOption = new Option<bool>(
            new[] { "--details", "-d" },
            getDefaultValue: () => false,
            "Show detailed information about each template set");

        var includeEmptyOption = new Option<bool>(
            new[] { "--include-empty", "-e" },
            getDefaultValue: () => false,
            "Include empty folders (folders without .docx files)");

        AddOption(templatesOption);
        AddOption(formatOption);
        AddOption(detailsOption);
        AddOption(includeEmptyOption);

        this.SetHandler(async (context) =>
        {
            var templatesPath = context.ParseResult.GetValueForOption(templatesOption)!;
            var format = context.ParseResult.GetValueForOption(formatOption);
            var showDetails = context.ParseResult.GetValueForOption(detailsOption);
            var includeEmpty = context.ParseResult.GetValueForOption(includeEmptyOption);
            var cancellationToken = context.GetCancellationToken();

            var serviceProvider = (IServiceProvider)context.BindingContext.GetService(typeof(IServiceProvider))!;
            var templateSetService = serviceProvider.GetService(typeof(ITemplateSetService)) as ITemplateSetService;
            var logger = serviceProvider.GetService(typeof(ILogger<ListSetsCommand>)) as ILogger<ListSetsCommand>;

            if (templateSetService == null)
            {
                Console.Error.WriteLine("Error: Template set service not available");
                context.ExitCode = 1;
                return;
            }

            try
            {
                logger?.LogDebug("Listing template sets in: {Path}", templatesPath);
                
                var templateSets = await templateSetService.ListTemplateSetsAsync(
                    templatesPath, 
                    includeEmpty, 
                    cancellationToken);

                switch (format)
                {
                    case OutputFormat.Json:
                        await OutputJsonFormat(templateSets, showDetails);
                        break;
                    case OutputFormat.Table:
                        OutputTableFormat(templateSets, showDetails);
                        break;
                    case OutputFormat.List:
                        OutputListFormat(templateSets);
                        break;
                    case OutputFormat.Text:
                    default:
                        OutputTextFormat(templateSets, templatesPath, showDetails);
                        break;
                }

                logger?.LogInformation("Successfully listed {Count} template set(s)", templateSets.Count);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to list template sets");
                Console.Error.WriteLine($"Error: {ex.Message}");
                context.ExitCode = 1;
            }
        });
    }

    private static async Task OutputJsonFormat(IReadOnlyList<Core.Models.TemplateSet> templateSets, bool showDetails)
    {
        var output = new
        {
            command = "list-sets",
            timestamp = DateTime.UtcNow,
            success = true,
            data = new
            {
                template_sets = templateSets.Select(ts => new
                {
                    name = ts.Name,
                    path = ts.FullPath,
                    file_count = ts.TemplateCount,
                    total_size = ts.TotalSizeBytes,
                    total_size_formatted = ts.DisplaySize,
                    last_modified = ts.LastModified,
                    status = ts.IsValid() ? "valid" : "invalid",
                    has_subfolders = ts.HasSubfolders,
                    directory_depth = showDetails ? (int?)ts.DirectoryDepth : null,
                    templates = showDetails ? ts.Templates.Take(5).Select(t => new
                    {
                        file_name = t.FileName,
                        relative_path = t.RelativePath,
                        size = t.SizeBytes,
                        size_formatted = t.DisplaySize
                    }) : null
                }).ToList(),
                total_sets = templateSets.Count
            }
        };

        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await Console.Out.WriteLineAsync(json);
    }

    private static void OutputTableFormat(IReadOnlyList<Core.Models.TemplateSet> templateSets, bool showDetails)
    {
        // Header
        Console.WriteLine("╔════════════════════════════════╤═══════════╤═══════════╤════════════════════╤══════════╗");
        Console.WriteLine("║ Template Set Name              │ Files     │ Size      │ Last Modified      │ Status   ║");
        Console.WriteLine("╟────────────────────────────────┼───────────┼───────────┼────────────────────┼──────────╢");

        foreach (var ts in templateSets.OrderBy(t => t.Name))
        {
            var name = ts.Name.Length > 30 ? ts.Name.Substring(0, 27) + "..." : ts.Name;
            var status = ts.IsValid() ? "Valid" : "Invalid";
            var lastMod = ts.LastModified.ToString("yyyy-MM-dd HH:mm");

            Console.WriteLine($"║ {name,-30} │ {ts.TemplateCount,9} │ {ts.DisplaySize,9} │ {lastMod,18} │ {status,8} ║");

            if (showDetails && ts.HasSubfolders)
            {
                Console.WriteLine($"║   └─ Has subfolders (depth: {ts.DirectoryDepth})                                             ║");
            }
        }

        Console.WriteLine("╚════════════════════════════════╧═══════════╧═══════════╧════════════════════╧══════════╝");
        Console.WriteLine($"\nTotal: {templateSets.Count} template set(s)");
    }

    private static void OutputListFormat(IReadOnlyList<Core.Models.TemplateSet> templateSets)
    {
        foreach (var ts in templateSets.OrderBy(t => t.Name))
        {
            Console.WriteLine(ts.Name);
        }
    }

    private static void OutputTextFormat(IReadOnlyList<Core.Models.TemplateSet> templateSets, string templatesPath, bool showDetails)
    {
        Console.WriteLine($"Template Sets in {templatesPath}");
        Console.WriteLine(new string('=', Math.Min(80, templatesPath.Length + 18)));
        Console.WriteLine();

        if (templateSets.Count == 0)
        {
            Console.WriteLine("No template sets found.");
            return;
        }

        int index = 1;
        foreach (var ts in templateSets.OrderBy(t => t.Name))
        {
            Console.WriteLine($"{index}. {ts.Name} ({ts.TemplateCount} files, {ts.DisplaySize})");
            
            if (showDetails)
            {
                Console.WriteLine($"   Path: {ts.FullPath}");
                Console.WriteLine($"   Last Modified: {ts.LastModified:yyyy-MM-dd HH:mm:ss}");
                
                if (ts.HasSubfolders)
                {
                    Console.WriteLine($"   Directory Depth: {ts.DirectoryDepth} levels");
                }

                if (ts.Templates.Any())
                {
                    Console.WriteLine("   Sample files:");
                    foreach (var template in ts.Templates.Take(3))
                    {
                        Console.WriteLine($"     - {template.RelativePath} ({template.DisplaySize})");
                    }
                    
                    if (ts.Templates.Count > 3)
                    {
                        Console.WriteLine($"     ... and {ts.Templates.Count - 3} more");
                    }
                }

                Console.WriteLine();
            }
            
            index++;
        }

        if (!showDetails)
        {
            Console.WriteLine();
            Console.WriteLine($"Total: {templateSets.Count} template set(s)");
            Console.WriteLine($"Total size: {FormatBytes(templateSets.Sum(ts => ts.TotalSizeBytes))}");
            Console.WriteLine($"Total files: {templateSets.Sum(ts => ts.TemplateCount)}");
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

public enum OutputFormat
{
    Text,
    Json,
    Table,
    List
}