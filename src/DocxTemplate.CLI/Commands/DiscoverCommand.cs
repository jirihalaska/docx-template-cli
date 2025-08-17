using System.CommandLine;
using System.Globalization;
using System.Text;
using System.Text.Json;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Services;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.CLI.Commands;

/// <summary>
/// Command to discover all DOCX template files in a directory tree
/// </summary>
public class DiscoverCommand : Command
{
    public DiscoverCommand() : base("discover", "Discover all DOCX template files in a directory")
    {
        var pathOption = new Option<string>(
            new[] { "--path", "-p" },
            "Path to the directory to scan for templates")
        {
            IsRequired = true
        };

        var recursiveOption = new Option<bool>(
            new[] { "--recursive", "-r" },
            getDefaultValue: () => true,
            "Scan subdirectories recursively");

        var formatOption = new Option<OutputFormat>(
            new[] { "--format", "-f" },
            getDefaultValue: () => OutputFormat.Text,
            "Output format (text, json, table, csv)");

        var includeOption = new Option<string[]>(
            new[] { "--include", "-i" },
            getDefaultValue: () => new[] { "*.docx" },
            "File patterns to include")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var excludeOption = new Option<string[]>(
            new[] { "--exclude", "-e" },
            getDefaultValue: () => Array.Empty<string>(),
            "File patterns to exclude")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var maxDepthOption = new Option<int?>(
            new[] { "--max-depth", "-d" },
            "Maximum directory depth to scan");

        var minSizeOption = new Option<long?>(
            new[] { "--min-size" },
            "Minimum file size in bytes");

        var maxSizeOption = new Option<long?>(
            new[] { "--max-size" },
            "Maximum file size in bytes");

        var modifiedAfterOption = new Option<DateTime?>(
            new[] { "--modified-after" },
            "Only show files modified after this date");

        var modifiedBeforeOption = new Option<DateTime?>(
            new[] { "--modified-before" },
            "Only show files modified before this date");

        var quietOption = new Option<bool>(
            new[] { "--quiet", "-q" },
            getDefaultValue: () => false,
            "Suppress progress messages");

        AddOption(pathOption);
        AddOption(recursiveOption);
        AddOption(formatOption);
        AddOption(includeOption);
        AddOption(excludeOption);
        AddOption(maxDepthOption);
        AddOption(minSizeOption);
        AddOption(maxSizeOption);
        AddOption(modifiedAfterOption);
        AddOption(modifiedBeforeOption);
        AddOption(quietOption);

        this.SetHandler(async (context) =>
        {
            var path = context.ParseResult.GetValueForOption(pathOption)!;
            var recursive = context.ParseResult.GetValueForOption(recursiveOption);
            var format = context.ParseResult.GetValueForOption(formatOption);
            var includePatterns = context.ParseResult.GetValueForOption(includeOption) ?? new[] { "*.docx" };
            var excludePatterns = context.ParseResult.GetValueForOption(excludeOption) ?? Array.Empty<string>();
            var maxDepth = context.ParseResult.GetValueForOption(maxDepthOption);
            var minSize = context.ParseResult.GetValueForOption(minSizeOption);
            var maxSize = context.ParseResult.GetValueForOption(maxSizeOption);
            var modifiedAfter = context.ParseResult.GetValueForOption(modifiedAfterOption);
            var modifiedBefore = context.ParseResult.GetValueForOption(modifiedBeforeOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var cancellationToken = context.GetCancellationToken();

            var serviceProvider = (IServiceProvider)context.BindingContext.GetService(typeof(IServiceProvider))!;
            var discoveryService = serviceProvider.GetService(typeof(ITemplateDiscoveryService)) as ITemplateDiscoveryService;
            var logger = serviceProvider.GetService(typeof(ILogger<DiscoverCommand>)) as ILogger<DiscoverCommand>;

            if (discoveryService == null)
            {
                Console.Error.WriteLine("Error: Template discovery service not available");
                context.ExitCode = 1;
                return;
            }

            try
            {
                if (!quiet)
                {
                    Console.WriteLine($"Discovering templates in: {path}");
                    if (recursive)
                    {
                        Console.WriteLine($"Scanning recursively{(maxDepth.HasValue ? $" (max depth: {maxDepth})" : "")}...");
                    }
                }

                // Discover templates
                var allTemplates = await discoveryService.DiscoverTemplatesAsync(
                    path, 
                    includePatterns, 
                    recursive, 
                    cancellationToken);

                // Apply filters
                var filteredTemplates = ApplyFilters(
                    allTemplates, 
                    excludePatterns, 
                    path,
                    maxDepth, 
                    minSize, 
                    maxSize, 
                    modifiedAfter, 
                    modifiedBefore);

                if (!quiet)
                {
                    Console.WriteLine($"Found {filteredTemplates.Count} template(s)");
                    Console.WriteLine();
                }

                // Output results
                switch (format)
                {
                    case OutputFormat.Json:
                        await OutputJsonFormat(filteredTemplates);
                        break;
                    case OutputFormat.Table:
                        OutputTableFormat(filteredTemplates);
                        break;
                    case OutputFormat.Csv:
                        await OutputCsvFormat(filteredTemplates);
                        break;
                    case OutputFormat.Text:
                    default:
                        OutputTextFormat(filteredTemplates);
                        break;
                }

                logger?.LogInformation("Successfully discovered {Count} template(s)", filteredTemplates.Count);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to discover templates");
                Console.Error.WriteLine($"Error: {ex.Message}");
                context.ExitCode = 1;
            }
        });
    }

    private static List<TemplateFile> ApplyFilters(
        IReadOnlyList<TemplateFile> templates,
        string[] excludePatterns,
        string basePath,
        int? maxDepth,
        long? minSize,
        long? maxSize,
        DateTime? modifiedAfter,
        DateTime? modifiedBefore)
    {
        var filtered = templates.AsEnumerable();

        // Apply exclude patterns
        foreach (var pattern in excludePatterns)
        {
            filtered = filtered.Where(t => !IsPatternMatch(t.FileName, pattern));
        }

        // Apply max depth filter
        if (maxDepth.HasValue)
        {
            filtered = filtered.Where(t =>
            {
                var depth = GetPathDepth(t.RelativePath);
                return depth <= maxDepth.Value;
            });
        }

        // Apply size filters
        if (minSize.HasValue)
        {
            filtered = filtered.Where(t => t.SizeInBytes >= minSize.Value);
        }

        if (maxSize.HasValue)
        {
            filtered = filtered.Where(t => t.SizeInBytes <= maxSize.Value);
        }

        // Apply date filters
        if (modifiedAfter.HasValue)
        {
            filtered = filtered.Where(t => t.LastModified >= modifiedAfter.Value);
        }

        if (modifiedBefore.HasValue)
        {
            filtered = filtered.Where(t => t.LastModified <= modifiedBefore.Value);
        }

        return filtered.OrderBy(t => t.RelativePath).ToList();
    }

    private static int GetPathDepth(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return 0;

        return relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, 
            StringSplitOptions.RemoveEmptyEntries).Length - 1;
    }

    private static bool IsPatternMatch(string fileName, string pattern)
    {
        // Simple wildcard pattern matching
        pattern = pattern.Replace("*", ".*").Replace("?", ".");
        return System.Text.RegularExpressions.Regex.IsMatch(
            fileName, 
            $"^{pattern}$", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static async Task OutputJsonFormat(List<TemplateFile> templates)
    {
        var output = new
        {
            command = "discover",
            timestamp = DateTime.UtcNow,
            success = true,
            data = new
            {
                templates = templates.Select(t => new
                {
                    full_path = t.FullPath,
                    relative_path = t.RelativePath,
                    file_name = t.FileName,
                    size_bytes = t.SizeInBytes,
                    size_formatted = t.DisplaySize,
                    last_modified = t.LastModified,
                    directory = t.DirectoryName
                }).ToList(),
                total_count = templates.Count,
                total_size = templates.Sum(t => t.SizeInBytes),
                total_size_formatted = FormatBytes(templates.Sum(t => t.SizeInBytes))
            }
        };

        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await Console.Out.WriteLineAsync(json);
    }

    private static void OutputTableFormat(List<TemplateFile> templates)
    {
        if (templates.Count == 0)
        {
            Console.WriteLine("No templates found.");
            return;
        }

        // Header
        Console.WriteLine("╔═══════════════════════════════════════════════════════╤═══════════╤════════════════════╗");
        Console.WriteLine("║ File Path                                             │ Size      │ Last Modified      ║");
        Console.WriteLine("╟───────────────────────────────────────────────────────┼───────────┼────────────────────╢");

        foreach (var template in templates)
        {
            var path = template.RelativePath.Length > 50 
                ? "..." + template.RelativePath.Substring(template.RelativePath.Length - 47) 
                : template.RelativePath;
            var lastMod = template.LastModified.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

            Console.WriteLine($"║ {path,-53} │ {template.DisplaySize,9} │ {lastMod,18} ║");
        }

        Console.WriteLine("╚═══════════════════════════════════════════════════════╧═══════════╧════════════════════╝");
        Console.WriteLine($"\nTotal: {templates.Count} file(s), {FormatBytes(templates.Sum(t => t.SizeInBytes))}");
    }

    private static async Task OutputCsvFormat(List<TemplateFile> templates)
    {
        var csv = new StringBuilder();
        csv.AppendLine("RelativePath,FileName,SizeBytes,LastModified,Directory");

        foreach (var template in templates)
        {
            csv.AppendLine($"\"{template.RelativePath}\",\"{template.FileName}\",{template.SizeInBytes},{template.LastModified:yyyy-MM-dd HH:mm:ss},\"{template.DirectoryName}\"");
        }

        await Console.Out.WriteAsync(csv.ToString());
    }

    private static void OutputTextFormat(List<TemplateFile> templates)
    {
        if (templates.Count == 0)
        {
            Console.WriteLine("No templates found.");
            return;
        }

        foreach (var template in templates)
        {
            Console.WriteLine($"{template.RelativePath}");
            Console.WriteLine($"  Size: {template.DisplaySize}");
            Console.WriteLine($"  Modified: {template.LastModified:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();
        }

        Console.WriteLine($"Total: {templates.Count} file(s), {FormatBytes(templates.Sum(t => t.SizeInBytes))}");
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