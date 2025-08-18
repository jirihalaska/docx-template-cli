using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using System.Text.Json;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Models.Results;
using DocxTemplate.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.CLI.Commands;

/// <summary>
/// Command for replacing placeholders in DOCX files with values from a mapping file
/// </summary>
public class ReplaceCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the ReplaceCommand class
    /// </summary>
    public ReplaceCommand() : base("replace", "Replace placeholders in DOCX templates with actual values")
    {
        var folderOption = new Option<string>(
            aliases: ["--folder", "-f"],
            description: "Target directory containing copied templates")
        {
            IsRequired = true
        };

        var mapOption = new Option<string>(
            aliases: ["--map", "-m"],
            description: "JSON file containing placeholder mappings")
        {
            IsRequired = true
        };

        var backupOption = new Option<bool>(
            aliases: ["--backup", "-b"],
            getDefaultValue: () => true,
            description: "Create backups before replacement");

        var recursiveOption = new Option<bool>(
            aliases: ["--recursive", "-r"],
            getDefaultValue: () => true,
            description: "Include subdirectories");

        var dryRunOption = new Option<bool>(
            aliases: ["--dry-run", "-d"],
            getDefaultValue: () => false,
            description: "Preview replacements without modifying files");

        var formatOption = new Option<OutputFormat>(
            aliases: ["--format", "-o"],
            getDefaultValue: () => OutputFormat.Text,
            description: "Output format (text, json, table)");

        var quietOption = new Option<bool>(
            aliases: ["--quiet", "-q"],
            getDefaultValue: () => false,
            description: "Suppress progress messages");

        var patternOption = new Option<string>(
            aliases: ["--pattern", "-p"],
            getDefaultValue: () => @"\{\{.*?\}\}",
            description: "Placeholder pattern");

        AddOption(folderOption);
        AddOption(mapOption);
        AddOption(backupOption);
        AddOption(recursiveOption);
        AddOption(dryRunOption);
        AddOption(formatOption);
        AddOption(quietOption);
        AddOption(patternOption);

        this.SetHandler(async (context) =>
        {
            var folderPath = context.ParseResult.GetValueForOption(folderOption)!;
            var mapPath = context.ParseResult.GetValueForOption(mapOption)!;
            var createBackup = context.ParseResult.GetValueForOption(backupOption);
            var recursive = context.ParseResult.GetValueForOption(recursiveOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var format = context.ParseResult.GetValueForOption(formatOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var pattern = context.ParseResult.GetValueForOption(patternOption)!;

            var serviceProvider = (IServiceProvider)context.BindingContext.GetService(typeof(IServiceProvider))!;
            var cancellationToken = context.GetCancellationToken();

            await ExecuteAsync(
                serviceProvider,
                context,
                folderPath,
                mapPath,
                createBackup,
                recursive,
                dryRun,
                format,
                quiet,
                pattern,
                cancellationToken);
        });
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        InvocationContext context,
        string folderPath,
        string mapPath,
        bool createBackup,
        bool recursive,
        bool dryRun,
        OutputFormat format,
        bool quiet,
        string pattern,
        CancellationToken cancellationToken)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<ReplaceCommand>>();
        var replaceService = serviceProvider.GetRequiredService<IPlaceholderReplaceService>();
        var discoveryService = serviceProvider.GetRequiredService<ITemplateDiscoveryService>();

        try
        {
            // Validate folder path
            if (!Directory.Exists(folderPath))
            {
                logger.LogError("Folder path not found: {FolderPath}", folderPath);
                Console.Error.WriteLine($"Error: Folder path not found: {folderPath}");
                context.ExitCode = 1;
                return;
            }

            // Validate and load mapping file
            if (!File.Exists(mapPath))
            {
                logger.LogError("Mapping file not found: {MapPath}", mapPath);
                Console.Error.WriteLine($"Error: Mapping file not found: {mapPath}");
                context.ExitCode = 1;
                return;
            }

            ReplacementMap replacementMap;
            try
            {
                var jsonContent = await File.ReadAllTextAsync(mapPath, cancellationToken);
                var jsonDocument = JsonDocument.Parse(jsonContent);

                var mappings = new Dictionary<string, string>();
                if (jsonDocument.RootElement.TryGetProperty("placeholders", out var placeholdersElement))
                {
                    foreach (var property in placeholdersElement.EnumerateObject())
                    {
                        mappings[property.Name] = property.Value.GetString() ?? string.Empty;
                    }
                }

                replacementMap = new ReplacementMap
                {
                    Mappings = mappings,
                    SourceFilePath = mapPath
                };
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Invalid JSON in mapping file: {MapPath}", mapPath);
                Console.Error.WriteLine($"Error: Invalid JSON in mapping file: {ex.Message}");
                context.ExitCode = 1;
                return;
            }

            if (!quiet)
            {
                Console.WriteLine($"Processing templates in: {folderPath}");
                Console.WriteLine($"Using mapping file: {mapPath}");
                Console.WriteLine($"Placeholders to replace: {replacementMap.Count}");
            }

            // Discover templates
            var templates = await discoveryService.DiscoverTemplatesAsync(folderPath, recursive, cancellationToken);

            if (!templates.Any())
            {
                if (!quiet)
                {
                    Console.WriteLine("No DOCX files found in the specified folder.");
                }
                return;
            }

            if (!quiet)
            {
                Console.WriteLine($"Found {templates.Count} template(s) to process");
            }

            // Preview or execute replacements
            if (dryRun)
            {
                if (!quiet)
                {
                    Console.WriteLine("\nDRY RUN MODE - No files will be modified\n");
                }

                var preview = await replaceService.PreviewReplacementsAsync(
                    folderPath,
                    replacementMap,
                    cancellationToken);

                FormatReplacementPreview(preview, format, quiet);
            }
            else
            {
                var result = await replaceService.ReplacePlaceholdersAsync(
                    templates,
                    replacementMap,
                    createBackup,
                    cancellationToken);

                FormatReplaceResult(result, format, quiet);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during replace operation");
            Console.Error.WriteLine($"Error: {ex.Message}");
            context.ExitCode = 1;
        }
    }

    private static void FormatReplaceResult(ReplaceResult result, OutputFormat format, bool quiet)
    {
        if (format == OutputFormat.Json)
        {
            var jsonOutput = new
            {
                command = "replace",
                timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                success = true,
                data = new
                {
                    summary = new
                    {
                        files_processed = result.FilesProcessed,
                        files_modified = result.SuccessfulFiles,
                        files_failed = result.FailedFiles,
                        total_replacements = result.TotalReplacements,
                        backup_created = true,
                        duration_ms = result.Duration.TotalMilliseconds
                    },
                    file_results = result.FileResults.Select(f => new
                    {
                        file_path = f.FilePath,
                        replacements_made = f.ReplacementCount,
                        backup_path = f.BackupPath,
                        status = f.IsSuccess ? "success" : "failed",
                        error_message = f.ErrorMessage
                    })
                }
            };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            Console.WriteLine(JsonSerializer.Serialize(jsonOutput, jsonOptions));
        }
        else if (!quiet)
        {
            Console.WriteLine($"\nProcessed {result.FilesProcessed} file(s)");
            Console.WriteLine($"Modified {result.SuccessfulFiles} file(s)");
            Console.WriteLine($"Total replacements: {result.TotalReplacements}");

            if (result.FailedFiles > 0)
            {
                Console.WriteLine($"Failed: {result.FailedFiles} file(s)");
            }
        }
    }

    private static void FormatReplacementPreview(ReplacementPreview preview, OutputFormat format, bool quiet)
    {
        if (format == OutputFormat.Json)
        {
            var jsonOutput = new
            {
                command = "replace",
                mode = "preview",
                timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                success = true,
                data = preview
            };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            Console.WriteLine(JsonSerializer.Serialize(jsonOutput, jsonOptions));
        }
        else if (!quiet)
        {
            Console.WriteLine("Replacement Preview:");
            Console.WriteLine($"  Files to process: {preview.FilesToProcess}");
            Console.WriteLine($"  Total replacements planned: {preview.TotalReplacements}");
            Console.WriteLine($"  Placeholders with mappings: {preview.MappedPlaceholders.Count}");
            Console.WriteLine($"  Missing mappings: {preview.UnmappedPlaceholders.Count}");

            if (preview.UnmappedPlaceholders.Any())
            {
                Console.WriteLine("\nMissing mappings for placeholders:");
                foreach (var missing in preview.UnmappedPlaceholders)
                {
                    Console.WriteLine($"  - {missing}");
                }
            }

            if (preview.FilePreviews.Any())
            {
                Console.WriteLine("\nPreview of replacements:");
                foreach (var item in preview.FilePreviews)
                {
                    Console.WriteLine($"\n  File: {item.FilePath}");
                    Console.WriteLine($"  Replacements to make: {item.ReplacementCount}");
                }
            }
        }
    }
}
