using System.CommandLine;
using System.Globalization;
using System.Text;
using System.Text.Json;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Services;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.CLI.Commands;

/// <summary>
/// Command to scan DOCX templates and identify placeholders within them
/// </summary>
public class ScanCommand : Command
{
    public ScanCommand() : base("scan", "Scan DOCX templates for placeholders")
    {
        var pathOption = new Option<string>(
            new[] { "--path", "-p" },
            "Path to the directory or file to scan for placeholders")
        {
            IsRequired = true
        };

        var recursiveOption = new Option<bool>(
            new[] { "--recursive", "-r" },
            getDefaultValue: () => true,
            "Scan subdirectories recursively");

        var patternOption = new Option<string[]>(
            new[] { "--pattern" },
            getDefaultValue: () => new[] { @"\{\{.*?\}\}" },
            "Regex patterns for placeholders (can be specified multiple times)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var formatOption = new Option<OutputFormat>(
            new[] { "--format", "-f" },
            getDefaultValue: () => OutputFormat.Text,
            "Output format (text, json, table, csv)");

        var statisticsOption = new Option<bool>(
            new[] { "--statistics", "-s" },
            getDefaultValue: () => false,
            "Include detailed statistics in output");

        var caseSensitiveOption = new Option<bool>(
            new[] { "--case-sensitive", "-c" },
            getDefaultValue: () => false,
            "Use case-sensitive pattern matching");

        var parallelismOption = new Option<int>(
            new[] { "--parallelism" },
            getDefaultValue: () => Environment.ProcessorCount,
            "Number of parallel threads for scanning");

        var quietOption = new Option<bool>(
            new[] { "--quiet", "-q" },
            getDefaultValue: () => false,
            "Suppress progress messages");

        AddOption(pathOption);
        AddOption(recursiveOption);
        AddOption(patternOption);
        AddOption(formatOption);
        AddOption(statisticsOption);
        AddOption(caseSensitiveOption);
        AddOption(parallelismOption);
        AddOption(quietOption);

        this.SetHandler(async (context) =>
        {
            var path = context.ParseResult.GetValueForOption(pathOption)!;
            var recursive = context.ParseResult.GetValueForOption(recursiveOption);
            var patterns = context.ParseResult.GetValueForOption(patternOption) ?? new[] { @"\{\{.*?\}\}" };
            var format = context.ParseResult.GetValueForOption(formatOption);
            var includeStatistics = context.ParseResult.GetValueForOption(statisticsOption);
            var caseSensitive = context.ParseResult.GetValueForOption(caseSensitiveOption);
            var parallelism = context.ParseResult.GetValueForOption(parallelismOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var cancellationToken = context.GetCancellationToken();

            var serviceProvider = (IServiceProvider)context.BindingContext.GetService(typeof(IServiceProvider))!;
            var scanService = serviceProvider.GetService(typeof(IPlaceholderScanService)) as IPlaceholderScanService;
            var logger = serviceProvider.GetService(typeof(ILogger<ScanCommand>)) as ILogger<ScanCommand>;

            if (scanService == null)
            {
                Console.Error.WriteLine("Error: Placeholder scan service not available");
                context.ExitCode = 1;
                return;
            }

            try
            {
                if (!quiet)
                {
                    Console.WriteLine($"Scanning for placeholders in: {path}");
                    Console.WriteLine($"Patterns: {string.Join(", ", patterns)}");
                    if (recursive)
                    {
                        Console.WriteLine("Scanning recursively...");
                    }
                }

                PlaceholderScanResult? combinedResult = null;

                // Process each pattern and combine results
                foreach (var pattern in patterns)
                {
                    var adjustedPattern = caseSensitive ? pattern : MakePatternCaseInsensitive(pattern);
                    
                    try
                    {
                        // Validate pattern first
                        if (!scanService.ValidatePattern(adjustedPattern))
                        {
                            Console.Error.WriteLine($"Invalid pattern: {pattern}");
                            context.ExitCode = 1;
                            return;
                        }

                        PlaceholderScanResult scanResult;
                        
                        if (File.Exists(path))
                        {
                            // Scan single file
                            var placeholders = await scanService.ScanSingleFileAsync(path, adjustedPattern, cancellationToken);
                            scanResult = PlaceholderScanResult.Success(placeholders, 1, TimeSpan.Zero, placeholders.Any() ? 1 : 0);
                        }
                        else
                        {
                            // Scan directory
                            scanResult = await scanService.ScanPlaceholdersAsync(path, adjustedPattern, recursive, cancellationToken);
                        }

                        // Combine results if this is not the first pattern
                        if (combinedResult == null)
                        {
                            combinedResult = scanResult;
                        }
                        else
                        {
                            combinedResult = CombineScanResults(combinedResult, scanResult);
                        }
                    }
                    catch (Exception ex) when (ex.Message.Contains("pattern"))
                    {
                        Console.Error.WriteLine($"Invalid pattern '{pattern}': {ex.Message}");
                        context.ExitCode = 1;
                        return;
                    }
                }

                if (combinedResult == null)
                {
                    Console.Error.WriteLine("No scan results available");
                    context.ExitCode = 1;
                    return;
                }

                if (!quiet)
                {
                    Console.WriteLine($"Found {combinedResult.UniquePlaceholderCount} unique placeholder(s) in {combinedResult.TotalFilesScanned} file(s)");
                    if (combinedResult.FailedFiles > 0)
                    {
                        Console.WriteLine($"Warning: {combinedResult.FailedFiles} file(s) failed to scan");
                    }
                    Console.WriteLine();
                }

                // Output results
                switch (format)
                {
                    case OutputFormat.Json:
                        await OutputJsonFormat(combinedResult, includeStatistics, scanService);
                        break;
                    case OutputFormat.Table:
                        OutputTableFormat(combinedResult, includeStatistics, scanService);
                        break;
                    case OutputFormat.Csv:
                        await OutputCsvFormat(combinedResult);
                        break;
                    case OutputFormat.Text:
                    default:
                        OutputTextFormat(combinedResult, includeStatistics, scanService);
                        break;
                }

                logger?.LogInformation("Successfully scanned for placeholders, found {Count} unique placeholder(s)", 
                    combinedResult.UniquePlaceholderCount);

                // Set exit code based on results
                if (combinedResult.FailedFiles > 0)
                {
                    context.ExitCode = 2; // Partial failure
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to scan for placeholders");
                Console.Error.WriteLine($"Error: {ex.Message}");
                context.ExitCode = 1;
            }
        });
    }

    private static string MakePatternCaseInsensitive(string pattern)
    {
        // Simple approach: if pattern doesn't already have case insensitive flags, we'll handle it in the service
        return pattern;
    }

    private static PlaceholderScanResult CombineScanResults(PlaceholderScanResult first, PlaceholderScanResult second)
    {
        var combinedPlaceholders = new Dictionary<string, Placeholder>();
        
        // Add placeholders from first result
        foreach (var placeholder in first.Placeholders)
        {
            combinedPlaceholders[placeholder.Name] = placeholder;
        }

        // Merge placeholders from second result
        foreach (var placeholder in second.Placeholders)
        {
            if (combinedPlaceholders.ContainsKey(placeholder.Name))
            {
                var existing = combinedPlaceholders[placeholder.Name];
                var combinedLocations = existing.Locations.Concat(placeholder.Locations).ToList();
                
                combinedPlaceholders[placeholder.Name] = existing with
                {
                    Locations = combinedLocations.AsReadOnly(),
                    TotalOccurrences = combinedLocations.Sum(l => l.Occurrences)
                };
            }
            else
            {
                combinedPlaceholders[placeholder.Name] = placeholder;
            }
        }

        var combinedErrors = first.Errors.Concat(second.Errors).ToList();
        var totalDuration = first.ScanDuration + second.ScanDuration;

        return PlaceholderScanResult.WithErrors(
            combinedPlaceholders.Values.ToList(),
            Math.Max(first.TotalFilesScanned, second.TotalFilesScanned),
            totalDuration,
            Math.Max(first.FilesWithPlaceholders, second.FilesWithPlaceholders),
            first.FailedFiles + second.FailedFiles,
            combinedErrors
        );
    }

    private static async Task OutputJsonFormat(PlaceholderScanResult result, bool includeStatistics, IPlaceholderScanService scanService)
    {
        var output = new
        {
            command = "scan",
            timestamp = DateTime.UtcNow,
            success = result.IsSuccessful,
            data = new
            {
                placeholders = result.Placeholders.Select(p => new
                {
                    name = p.Name,
                    pattern = p.Pattern,
                    total_occurrences = p.TotalOccurrences,
                    unique_files = p.UniqueFileCount,
                    locations = p.Locations.Select(l => new
                    {
                        file_name = l.FileName,
                        file_path = l.FilePath,
                        occurrences = l.Occurrences,
                        context = l.Context
                    }).ToList()
                }).ToList(),
                summary = new
                {
                    unique_placeholders = result.UniquePlaceholderCount,
                    total_occurrences = result.TotalOccurrences,
                    files_scanned = result.TotalFilesScanned,
                    files_with_placeholders = result.FilesWithPlaceholders,
                    failed_files = result.FailedFiles,
                    scan_duration_ms = result.ScanDuration.TotalMilliseconds,
                    coverage_percentage = result.PlaceholderCoveragePercentage
                },
                statistics = includeStatistics ? new
                {
                    most_common = scanService.GetPlaceholderStatistics(result).MostCommonPlaceholders.Take(5).Select(p => new
                    {
                        name = p.Name,
                        occurrences = p.TotalOccurrences,
                        files = p.UniqueFileCount
                    }).ToList()
                } : null,
                errors = result.Errors.Select(e => new
                {
                    file_path = e.FilePath,
                    message = e.Message,
                    exception_type = e.ExceptionType
                }).ToList()
            }
        };

        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await Console.Out.WriteLineAsync(json);
    }

    private static void OutputTableFormat(PlaceholderScanResult result, bool includeStatistics, IPlaceholderScanService scanService)
    {
        if (result.Placeholders.Count == 0)
        {
            Console.WriteLine("No placeholders found.");
            return;
        }

        // Header
        Console.WriteLine("╔═══════════════════════════════════════════════════════╤═══════════╤═══════════╗");
        Console.WriteLine("║ Placeholder Name                                      │ Count     │ Files     ║");
        Console.WriteLine("╟───────────────────────────────────────────────────────┼───────────┼───────────╢");

        foreach (var placeholder in result.Placeholders.OrderByDescending(p => p.TotalOccurrences))
        {
            var name = placeholder.Name.Length > 50 
                ? placeholder.Name.Substring(0, 47) + "..." 
                : placeholder.Name;

            Console.WriteLine($"║ {name,-53} │ {placeholder.TotalOccurrences,9} │ {placeholder.UniqueFileCount,9} ║");
        }

        Console.WriteLine("╚═══════════════════════════════════════════════════════╧═══════════╧═══════════╝");
        Console.WriteLine($"\nTotal: {result.UniquePlaceholderCount} unique placeholders, {result.TotalOccurrences} occurrences");

        if (includeStatistics)
        {
            var stats = scanService.GetPlaceholderStatistics(result);
            Console.WriteLine("\nStatistics:");
            Console.WriteLine($"  Coverage: {stats.CoveragePercentage:F1}% ({stats.FilesWithPlaceholders}/{stats.FilesScanned} files)");
            Console.WriteLine($"  Average per file: {stats.AveragePerFile:F1} placeholders");
            Console.WriteLine($"  Scan rate: {stats.FilesPerSecond:F1} files/second");
        }
    }

    private static async Task OutputCsvFormat(PlaceholderScanResult result)
    {
        var csv = new StringBuilder();
        csv.AppendLine("PlaceholderName,Pattern,TotalOccurrences,UniqueFiles,FilePath,FileName,Occurrences,Context");

        foreach (var placeholder in result.Placeholders)
        {
            foreach (var location in placeholder.Locations)
            {
                csv.AppendLine($"\"{placeholder.Name}\",\"{placeholder.Pattern}\",{placeholder.TotalOccurrences},{placeholder.UniqueFileCount},\"{location.FilePath}\",\"{location.FileName}\",{location.Occurrences},\"{location.Context?.Replace("\"", "\"\"")}\"");
            }
        }

        await Console.Out.WriteAsync(csv.ToString());
    }

    private static void OutputTextFormat(PlaceholderScanResult result, bool includeStatistics, IPlaceholderScanService scanService)
    {
        if (result.Placeholders.Count == 0)
        {
            Console.WriteLine("No placeholders found.");
            return;
        }

        foreach (var placeholder in result.Placeholders.OrderByDescending(p => p.TotalOccurrences))
        {
            Console.WriteLine($"📍 {placeholder.Name}");
            Console.WriteLine($"   Pattern: {placeholder.Pattern}");
            Console.WriteLine($"   Occurrences: {placeholder.TotalOccurrences} in {placeholder.UniqueFileCount} file(s)");
            
            var groupedLocations = placeholder.Locations.GroupBy(l => l.FilePath);
            foreach (var fileGroup in groupedLocations.Take(3)) // Show max 3 files per placeholder
            {
                var fileName = Path.GetFileName(fileGroup.Key);
                var totalInFile = fileGroup.Sum(l => l.Occurrences);
                Console.WriteLine($"   • {fileName}: {totalInFile} occurrence(s)");
                
                foreach (var location in fileGroup.Take(2)) // Show max 2 locations per file
                {
                    Console.WriteLine($"     - {location.Occurrences}x occurrences");
                    if (!string.IsNullOrEmpty(location.Context))
                    {
                        Console.WriteLine($"       \"{location.Context.Trim()}\"");
                    }
                }
            }
            
            if (groupedLocations.Count() > 3)
            {
                Console.WriteLine($"   ... and {groupedLocations.Count() - 3} more file(s)");
            }
            
            Console.WriteLine();
        }

        Console.WriteLine($"Summary: {result.UniquePlaceholderCount} unique placeholders, {result.TotalOccurrences} total occurrences");
        Console.WriteLine($"Scan completed in {result.ScanDuration.TotalMilliseconds:F0}ms");

        if (includeStatistics)
        {
            var stats = scanService.GetPlaceholderStatistics(result);
            Console.WriteLine("\nDetailed Statistics:");
            Console.WriteLine($"  Files scanned: {stats.FilesScanned}");
            Console.WriteLine($"  Files with placeholders: {stats.FilesWithPlaceholders} ({stats.CoveragePercentage:F1}%)");
            Console.WriteLine($"  Average placeholders per file: {stats.AveragePerFile:F1}");
            Console.WriteLine($"  Scan rate: {stats.FilesPerSecond:F1} files/second");
            
            if (stats.MostCommonPlaceholders.Any())
            {
                Console.WriteLine("\n  Most common placeholders:");
                foreach (var common in stats.MostCommonPlaceholders.Take(5))
                {
                    Console.WriteLine($"    • {common.Name}: {common.TotalOccurrences} occurrences");
                }
            }
        }

        if (result.Errors.Any())
        {
            Console.WriteLine("\nErrors:");
            foreach (var error in result.Errors.Take(10))
            {
                Console.WriteLine($"  ❌ {error.DisplayMessage}");
            }
            
            if (result.Errors.Count > 10)
            {
                Console.WriteLine($"  ... and {result.Errors.Count - 10} more error(s)");
            }
        }
    }
}