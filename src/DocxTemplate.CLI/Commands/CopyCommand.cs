using System.CommandLine;
using System.Text;
using System.Text.Json;
using DocxTemplate.Core.Models.Results;
using DocxTemplate.Core.Services;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.CLI.Commands;

/// <summary>
/// Command to copy DOCX templates from source to target locations
/// </summary>
public class CopyCommand : Command
{
    public CopyCommand() : base("copy", "Copy DOCX templates to target directory")
    {
        var sourceOption = new Option<string>(
            new[] { "--source", "-s" },
            "Source directory path containing templates to copy")
        {
            IsRequired = true
        };

        var targetOption = new Option<string>(
            new[] { "--target", "-t" },
            "Target directory path where templates will be copied")
        {
            IsRequired = true
        };

        var preserveStructureOption = new Option<bool>(
            new[] { "--preserve-structure", "-p" },
            getDefaultValue: () => true,
            "Preserve directory structure in target location");

        var overwriteOption = new Option<bool>(
            new[] { "--overwrite", "--force", "-f" },
            getDefaultValue: () => false,
            "Overwrite existing files in target location");

        var dryRunOption = new Option<bool>(
            new[] { "--dry-run", "-d" },
            getDefaultValue: () => false,
            "Show what would be copied without performing actual operations");

        var formatOption = new Option<OutputFormat>(
            new[] { "--format", "-o" },
            getDefaultValue: () => OutputFormat.Text,
            "Output format (text, json, table, csv)");

        var quietOption = new Option<bool>(
            new[] { "--quiet", "-q" },
            getDefaultValue: () => false,
            "Suppress progress messages");

        var validateOption = new Option<bool>(
            new[] { "--validate", "-v" },
            getDefaultValue: () => false,
            "Validate copy operation before executing");

        var showEstimateOption = new Option<bool>(
            new[] { "--estimate", "-e" },
            getDefaultValue: () => false,
            "Show disk space estimate for the copy operation");

        AddOption(sourceOption);
        AddOption(targetOption);
        AddOption(preserveStructureOption);
        AddOption(overwriteOption);
        AddOption(dryRunOption);
        AddOption(formatOption);
        AddOption(quietOption);
        AddOption(validateOption);
        AddOption(showEstimateOption);

        this.SetHandler(async (context) =>
        {
            var source = context.ParseResult.GetValueForOption(sourceOption)!;
            var target = context.ParseResult.GetValueForOption(targetOption)!;
            var preserveStructure = context.ParseResult.GetValueForOption(preserveStructureOption);
            var overwrite = context.ParseResult.GetValueForOption(overwriteOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var format = context.ParseResult.GetValueForOption(formatOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var validate = context.ParseResult.GetValueForOption(validateOption);
            var showEstimate = context.ParseResult.GetValueForOption(showEstimateOption);
            var cancellationToken = context.GetCancellationToken();

            var serviceProvider = (IServiceProvider)context.BindingContext.GetService(typeof(IServiceProvider))!;
            var copyService = serviceProvider.GetService(typeof(ITemplateCopyService)) as ITemplateCopyService;
            var discoveryService = serviceProvider.GetService(typeof(ITemplateDiscoveryService)) as ITemplateDiscoveryService;
            var logger = serviceProvider.GetService(typeof(ILogger<CopyCommand>)) as ILogger<CopyCommand>;

            if (copyService == null)
            {
                Console.Error.WriteLine("Error: Template copy service not available");
                context.ExitCode = 1;
                return;
            }

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
                    Console.WriteLine($"Copy operation: {source} -> {target}");
                    Console.WriteLine($"Preserve structure: {preserveStructure}");
                    Console.WriteLine($"Overwrite: {overwrite}");
                    if (dryRun)
                    {
                        Console.WriteLine("DRY RUN MODE - No files will be copied");
                    }
                    Console.WriteLine();
                }

                // Validate operation if requested or if dry-run
                CopyValidationResult? validationResult = null;
                if (validate || dryRun)
                {
                    if (!quiet)
                    {
                        Console.WriteLine("Validating copy operation...");
                    }

                    validationResult = await copyService.ValidateCopyOperationAsync(source, target, overwrite, cancellationToken);

                    if (!validationResult.CanCopy && !dryRun)
                    {
                        Console.Error.WriteLine("Copy operation validation failed:");
                        foreach (var error in validationResult.Errors)
                        {
                            Console.Error.WriteLine($"  ERROR: {error}");
                        }

                        foreach (var warning in validationResult.Warnings)
                        {
                            Console.WriteLine($"  WARNING: {warning}");
                        }

                        context.ExitCode = 1;
                        return;
                    }

                    if (!quiet)
                    {
                        Console.WriteLine($"Validation result: {validationResult.GetSummary()}");
                        if (validationResult.Warnings.Count > 0)
                        {
                            foreach (var warning in validationResult.Warnings)
                            {
                                Console.WriteLine($"  WARNING: {warning}");
                            }
                        }
                        Console.WriteLine();
                    }
                }

                // Show space estimate if requested
                if (showEstimate)
                {
                    if (!quiet)
                    {
                        Console.WriteLine("Calculating space estimate...");
                    }

                    var templateFiles = await discoveryService.DiscoverTemplatesAsync(source, recursive: true, cancellationToken);
                    var estimate = await copyService.EstimateCopySpaceAsync(templateFiles, target, preserveStructure);

                    Console.WriteLine("Space Estimate:");
                    Console.WriteLine(estimate.GetSummary());
                    Console.WriteLine();
                    Console.WriteLine("Detailed Breakdown:");
                    Console.WriteLine(estimate.GetDetailedBreakdown());
                    Console.WriteLine();

                    if (!estimate.HasSufficientSpace)
                    {
                        Console.Error.WriteLine("WARNING: Insufficient disk space for copy operation!");
                        if (!overwrite)
                        {
                            context.ExitCode = 2;
                            return;
                        }
                    }
                }

                // Exit early if dry-run
                if (dryRun)
                {
                    if (validationResult != null)
                    {
                        await OutputDryRunResults(validationResult, format);
                    }
                    else
                    {
                        Console.WriteLine("Dry run completed. Use --validate to see detailed preview.");
                    }
                    return;
                }

                // Perform the actual copy operation
                if (!quiet)
                {
                    Console.WriteLine("Starting copy operation...");
                }

                var copyResult = await copyService.CopyTemplatesAsync(source, target, preserveStructure, overwrite, cancellationToken);

                if (!quiet)
                {
                    Console.WriteLine($"Copy operation completed: {copyResult.GetSummary()}");
                    
                    if (copyResult.FailedFiles > 0)
                    {
                        Console.WriteLine($"Warnings: {copyResult.FailedFiles} file(s) failed to copy");
                    }
                    Console.WriteLine();
                }

                // Output results in requested format
                switch (format)
                {
                    case OutputFormat.Json:
                        await OutputJsonFormat(copyResult);
                        break;
                    case OutputFormat.Table:
                        OutputTableFormat(copyResult);
                        break;
                    case OutputFormat.Csv:
                        await OutputCsvFormat(copyResult);
                        break;
                    case OutputFormat.Text:
                    default:
                        OutputTextFormat(copyResult);
                        break;
                }

                logger?.LogInformation("Copy operation completed successfully. Copied {Count} files", copyResult.FilesCount);

                // Set exit code based on results
                if (copyResult.FailedFiles > 0)
                {
                    context.ExitCode = 2; // Partial failure
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Copy operation failed");
                Console.Error.WriteLine($"Error: {ex.Message}");
                context.ExitCode = 1;
            }
        });
    }

    private static async Task OutputDryRunResults(CopyValidationResult validationResult, OutputFormat format)
    {
        var dryRunData = new
        {
            command = "copy",
            mode = "dry-run",
            timestamp = DateTime.UtcNow,
            validation = new
            {
                can_copy = validationResult.CanCopy,
                files_to_copy = validationResult.FilesToCopy,
                files_to_overwrite = validationResult.FilesToOverwrite,
                directories_to_create = validationResult.DirectoriesToCreate,
                total_size_bytes = validationResult.TotalSizeBytes,
                total_size_display = validationResult.DisplayTotalSize,
                available_space_bytes = validationResult.AvailableSpaceBytes,
                available_space_display = validationResult.DisplayAvailableSpace,
                space_usage_percentage = validationResult.SpaceUsagePercentage,
                has_sufficient_space = validationResult.HasSufficientSpace,
                errors = validationResult.Errors,
                warnings = validationResult.Warnings,
                conflicting_files = validationResult.ConflictingFiles.Select(cf => new
                {
                    source_file = cf.SourceFilePath,
                    target_file = cf.TargetFilePath,
                    conflict_type = cf.ConflictType.ToString(),
                    source_size = cf.SourceSizeBytes,
                    target_size = cf.TargetSizeBytes,
                    source_newer = cf.IsSourceNewer,
                    source_larger = cf.IsSourceLarger
                })
            }
        };

        switch (format)
        {
            case OutputFormat.Json:
                var json = JsonSerializer.Serialize(dryRunData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await Console.Out.WriteLineAsync(json);
                break;

            default:
                Console.WriteLine("Dry Run Results:");
                Console.WriteLine(validationResult.GetSummary());
                if (validationResult.ConflictingFiles.Count > 0)
                {
                    Console.WriteLine("\nFile Conflicts:");
                    foreach (var conflict in validationResult.ConflictingFiles.Take(10))
                    {
                        Console.WriteLine($"  • {conflict.DisplayConflict}: {Path.GetFileName(conflict.SourceFilePath)}");
                    }
                    if (validationResult.ConflictingFiles.Count > 10)
                    {
                        Console.WriteLine($"  ... and {validationResult.ConflictingFiles.Count - 10} more conflict(s)");
                    }
                }
                break;
        }
    }

    private static async Task OutputJsonFormat(CopyResult result)
    {
        var output = new
        {
            command = "copy",
            timestamp = DateTime.UtcNow,
            success = result.IsCompletelySuccessful,
            data = new
            {
                summary = new
                {
                    files_copied = result.FilesCount,
                    files_failed = result.FailedFiles,
                    total_files_attempted = result.TotalFilesAttempted,
                    total_bytes_copied = result.TotalBytesCount,
                    total_size_display = result.DisplayTotalSize,
                    duration_ms = result.Duration.TotalMilliseconds,
                    success_rate_percentage = result.SuccessRatePercentage,
                    throughput_display = result.DisplayThroughput,
                    files_per_second = result.FilesPerSecond,
                    bytes_per_second = result.BytesPerSecond,
                    average_file_size = result.AverageFileSize
                },
                copied_files = result.CopiedFiles.Select(cf => new
                {
                    source_path = cf.SourcePath,
                    target_path = cf.TargetPath,
                    size_bytes = cf.SizeInBytes,
                    size_display = cf.DisplaySize,
                    copy_duration_ms = cf.CopyDuration?.TotalMilliseconds,
                    copied_at = cf.CopiedAt
                }),
                errors = result.Errors.Select(e => new
                {
                    source_path = e.SourcePath,
                    target_path = e.TargetPath,
                    message = e.Message,
                    exception_type = e.ExceptionType,
                    timestamp = e.Timestamp
                })
            }
        };

        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await Console.Out.WriteLineAsync(json);
    }

    private static void OutputTableFormat(CopyResult result)
    {
        if (result.CopiedFiles.Count == 0)
        {
            Console.WriteLine("No files were copied.");
            return;
        }

        // Header
        Console.WriteLine("╔══════════════════════════════════════════════════════╤═══════════╤═══════════╗");
        Console.WriteLine("║ File Name                                            │ Size      │ Duration  ║");
        Console.WriteLine("╟──────────────────────────────────────────────────────┼───────────┼───────────╢");

        foreach (var file in result.CopiedFiles.Take(20)) // Show max 20 files
        {
            var fileName = Path.GetFileName(file.TargetPath);
            if (fileName.Length > 50)
            {
                fileName = fileName.Substring(0, 47) + "...";
            }

            var duration = file.CopyDuration?.TotalMilliseconds.ToString("F0") + "ms" ?? "N/A";
            if (duration.Length > 9)
            {
                duration = duration.Substring(0, 9);
            }

            Console.WriteLine($"║ {fileName,-52} │ {file.DisplaySize,9} │ {duration,9} ║");
        }

        Console.WriteLine("╚══════════════════════════════════════════════════════╧═══════════╧═══════════╝");
        
        if (result.CopiedFiles.Count > 20)
        {
            Console.WriteLine($"... and {result.CopiedFiles.Count - 20} more file(s)");
        }

        Console.WriteLine($"\nSummary: {result.GetSummary()}");
    }

    private static async Task OutputCsvFormat(CopyResult result)
    {
        var csv = new StringBuilder();
        csv.AppendLine("SourcePath,TargetPath,SizeBytes,SizeDisplay,CopyDurationMs,CopiedAt");

        foreach (var file in result.CopiedFiles)
        {
            csv.AppendLine($"\"{file.SourcePath}\",\"{file.TargetPath}\",{file.SizeInBytes},\"{file.DisplaySize}\",{file.CopyDuration?.TotalMilliseconds:F0},{file.CopiedAt:yyyy-MM-ddTHH:mm:ss.fffZ}");
        }

        await Console.Out.WriteAsync(csv.ToString());
    }

    private static void OutputTextFormat(CopyResult result)
    {
        if (result.CopiedFiles.Count == 0)
        {
            Console.WriteLine("No files were copied.");
            return;
        }

        Console.WriteLine("📁 Copy Results:");
        Console.WriteLine($"   Successfully copied: {result.FilesCount} files ({result.DisplayTotalSize})");
        Console.WriteLine($"   Duration: {result.Duration.TotalMilliseconds:F0}ms");
        Console.WriteLine($"   Average speed: {result.FilesPerSecond:F1} files/sec ({result.DisplayThroughput})");

        if (result.FailedFiles > 0)
        {
            Console.WriteLine($"   Failed copies: {result.FailedFiles} files");
        }

        Console.WriteLine($"   Success rate: {result.SuccessRatePercentage:F1}%");
        Console.WriteLine();

        // Show sample of copied files
        var sampleFiles = result.CopiedFiles.Take(5);
        Console.WriteLine("Sample copied files:");
        foreach (var file in sampleFiles)
        {
            var fileName = Path.GetFileName(file.TargetPath);
            var duration = file.CopyDuration?.TotalMilliseconds.ToString("F0") + "ms" ?? "N/A";
            Console.WriteLine($"  • {fileName} ({file.DisplaySize}) - {duration}");
        }

        if (result.CopiedFiles.Count > 5)
        {
            Console.WriteLine($"  ... and {result.CopiedFiles.Count - 5} more file(s)");
        }

        // Show errors if any
        if (result.Errors.Count > 0)
        {
            Console.WriteLine("\nErrors:");
            foreach (var error in result.Errors.Take(5))
            {
                Console.WriteLine($"  ❌ {error.DisplayMessage}");
            }

            if (result.Errors.Count > 5)
            {
                Console.WriteLine($"  ... and {result.Errors.Count - 5} more error(s)");
            }
        }
    }
}