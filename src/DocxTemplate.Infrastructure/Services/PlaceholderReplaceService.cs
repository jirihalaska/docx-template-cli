using DocxTemplate.Core.ErrorHandling;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Models.Results;
using DocxTemplate.Core.Services;
using DocxTemplate.Infrastructure.DocxProcessing;
using DocxTemplate.Infrastructure.Images;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Pictures;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

using W = DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace DocxTemplate.Infrastructure.Services;

/// <summary>
/// Service for replacing placeholders in Word documents with actual values
/// </summary>
public class PlaceholderReplaceService : IPlaceholderReplaceService
{
    private readonly ILogger<PlaceholderReplaceService> _logger;
    private readonly IErrorHandler _errorHandler;
    private readonly IFileSystemService _fileSystemService;
    private readonly IImageProcessor _imageProcessor;
    private readonly DocumentTraverser _documentTraverser;
    private readonly PlaceholderProcessor _processor;
    private static readonly Regex PlaceholderPattern = new(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);
    private static readonly Regex ImagePlaceholderPattern = new(PlaceholderPatterns.ImagePlaceholderPattern, RegexOptions.Compiled);

    public PlaceholderReplaceService(
        ILogger<PlaceholderReplaceService> logger,
        IErrorHandler errorHandler,
        IFileSystemService fileSystemService,
        IImageProcessor imageProcessor,
        DocumentTraverser documentTraverser)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
        _documentTraverser = documentTraverser ?? throw new ArgumentNullException(nameof(documentTraverser));
        
        // Create processor with its own logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _processor = new PlaceholderProcessor(loggerFactory.CreateLogger<PlaceholderProcessor>());
    }

    /// <inheritdoc />
    public async Task<ReplaceResult> ReplacePlaceholdersAsync(
        string folderPath,
        ReplacementMap replacementMap,
        bool createBackup = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        ArgumentNullException.ThrowIfNull(replacementMap);

        try
        {

            if (!_fileSystemService.DirectoryExists(folderPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");
            }

            var templateFiles = await DiscoverTemplateFilesAsync(folderPath, cancellationToken);

            return await ReplacePlaceholdersAsync(templateFiles, replacementMap, createBackup, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorResult = await _errorHandler.HandleExceptionAsync(ex, "folder replacement");
            var fileResult = FileReplaceResult.Failure(folderPath, errorResult?.Message ?? ex.Message);
            return ReplaceResult.Success([fileResult], TimeSpan.Zero);
        }
    }

    /// <inheritdoc />
    public async Task<ReplaceResult> ReplacePlaceholdersAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        ReplacementMap replacementMap,
        bool createBackup = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateFiles);
        ArgumentNullException.ThrowIfNull(replacementMap);

        if (!replacementMap.IsValid())
        {
            throw new ArgumentException("Invalid replacement map", nameof(replacementMap));
        }

        try
        {

            var startTime = DateTime.UtcNow;
            var results = new List<FileReplaceResult>();

            // Create backups if requested and files exist
            if (createBackup && templateFiles.Count > 0)
            {
                var backupResult = await CreateBackupsAsync(templateFiles, cancellationToken: cancellationToken);
                if (!backupResult.IsCompletelySuccessful)
                {
                    _logger.LogWarning("Some backups failed, continuing with replacements");
                }
            }

            // Process each file
            foreach (var templateFile in templateFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var fileResult = await ReplacePlaceholdersInFileAsync(
                    templateFile.FullPath,
                    replacementMap,
                    false, // Already created backup above
                    cancellationToken);

                results.Add(fileResult);
            }

            var duration = DateTime.UtcNow - startTime;
            return ReplaceResult.Success(results, duration);
        }
        catch (Exception ex)
        {
            var errorResult = await _errorHandler.HandleExceptionAsync(ex, "batch replacement");
            var fileResult = FileReplaceResult.Failure("", errorResult?.Message ?? ex.Message);
            return ReplaceResult.Success([fileResult], TimeSpan.Zero);
        }
    }

    /// <inheritdoc />
    public async Task<FileReplaceResult> ReplacePlaceholdersInFileAsync(
        string templatePath,
        ReplacementMap replacementMap,
        bool createBackup = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templatePath);
        ArgumentNullException.ThrowIfNull(replacementMap);

        if (!replacementMap.IsValid())
        {
            throw new ArgumentException("Invalid replacement map", nameof(replacementMap));
        }

        try
        {

            if (!_fileSystemService.FileExists(templatePath))
            {
                return FileReplaceResult.Failure(templatePath, "File not found");
            }

            var startTime = DateTime.UtcNow;
            var backupPath = string.Empty;

            if (createBackup)
            {
                backupPath = await CreateFileBackupAsync(templatePath, cancellationToken);
            }

            var replacementCount = await ProcessDocumentReplacementsAsync(templatePath, replacementMap, cancellationToken);
            
            // Handle file prefix renaming if SOUBOR_PREFIX was provided
            var finalFilePath = templatePath;
            if (replacementMap.Mappings.TryGetValue(Placeholder.FilePrefixPlaceholder, out var prefix) && 
                !string.IsNullOrWhiteSpace(prefix))
            {
                finalFilePath = await ApplyFilePrefixAsync(templatePath, prefix, cancellationToken);
            }
            
            var endTime = DateTime.UtcNow;

            return FileReplaceResult.Success(
                finalFilePath,
                replacementCount,
                backupPath,
                endTime - startTime,
                _fileSystemService.GetFileSize(finalFilePath));
        }
        catch (Exception ex)
        {
            var errorResult = await _errorHandler.HandleExceptionAsync(ex, "file replacement");
            return FileReplaceResult.Failure(templatePath, errorResult?.Message ?? ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<ReplacementPreview> PreviewReplacementsAsync(
        string folderPath,
        ReplacementMap replacementMap,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        ArgumentNullException.ThrowIfNull(replacementMap);

        try
        {

            if (!_fileSystemService.DirectoryExists(folderPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");
            }

            var startTime = DateTime.UtcNow;
            var templateFiles = await DiscoverTemplateFilesAsync(folderPath, cancellationToken);
            var previews = new List<FileReplacementPreview>();
            var allPlaceholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var templateFile in templateFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var filePreview = await CreateFilePreviewAsync(templateFile.FullPath, replacementMap, cancellationToken);
                previews.Add(filePreview);

                // Collect all placeholders for summary
                foreach (var detail in filePreview.ReplacementDetails)
                {
                    allPlaceholders.Add(detail.PlaceholderName);
                }
            }

            var mappedPlaceholders = allPlaceholders.Where(p => replacementMap.Mappings.ContainsKey(p)).ToList();
            var unmappedPlaceholders = allPlaceholders.Except(mappedPlaceholders).ToList();
            var unusedMappings = replacementMap.PlaceholderNames.Except(allPlaceholders).ToList();

            var duration = DateTime.UtcNow - startTime;

            return ReplacementPreview.Create(previews, mappedPlaceholders, unmappedPlaceholders, unusedMappings, duration);
        }
        catch (Exception ex)
        {
            var errorResult = await _errorHandler.HandleExceptionAsync(ex, "replacement preview");
            throw new InvalidOperationException($"Preview failed: {errorResult?.Message ?? ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public ReplacementValidationResult ValidateReplacements(
        IReadOnlyList<Placeholder> placeholders,
        ReplacementMap replacementMap,
        bool requireAllPlaceholders = false)
    {
        ArgumentNullException.ThrowIfNull(placeholders);
        ArgumentNullException.ThrowIfNull(replacementMap);

        try
        {

            var startTime = DateTime.UtcNow;
            var errors = new List<string>();
            var warnings = new List<string>();
            var invalidMappings = new List<InvalidMapping>();

            // Check for placeholders without replacements
            var placeholderNames = placeholders.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var mappingNames = replacementMap.PlaceholderNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var validMappings = placeholderNames.Intersect(mappingNames).ToList();
            var missingReplacements = placeholderNames.Except(mappingNames).ToList();
            var unusedMappings = mappingNames.Except(placeholderNames).ToList();

            var missingRequired = requireAllPlaceholders ? missingReplacements : [];
            var missingOptional = requireAllPlaceholders ? [] : missingReplacements;

            if (requireAllPlaceholders && missingReplacements.Count > 0)
            {
                errors.AddRange(missingReplacements.Select(name => $"Missing replacement for required placeholder: {name}"));
            }
            else if (missingReplacements.Count > 0)
            {
                warnings.AddRange(missingReplacements.Select(name => $"No replacement provided for placeholder: {name}"));
            }

            if (unusedMappings.Count > 0)
            {
                warnings.AddRange(unusedMappings.Select(name => $"Unused replacement mapping: {name}"));
            }

            // Validate replacement values
            foreach (var mapping in replacementMap.Mappings)
            {
                if (!ReplacementMap.IsValidPlaceholderName(mapping.Key))
                {
                    errors.Add($"Invalid placeholder name: {mapping.Key}");
                    invalidMappings.Add(new InvalidMapping
                    {
                        PlaceholderName = mapping.Key,
                        ReplacementValue = mapping.Value,
                        ValidationError = "Invalid placeholder name format",
                        Severity = ValidationSeverity.Error
                    });
                }
            }

            var duration = DateTime.UtcNow - startTime;
            var isValid = errors.Count == 0;

            if (isValid)
            {
                return ReplacementValidationResult.Success(
                    validMappings,
                    missingOptional,
                    unusedMappings,
                    duration,
                    warnings);
            }
            else
            {
                return ReplacementValidationResult.Failure(
                    errors,
                    validMappings,
                    missingRequired,
                    missingOptional,
                    unusedMappings,
                    invalidMappings,
                    duration,
                    warnings);
            }
        }
        catch (Exception ex)
        {
            return ReplacementValidationResult.Failure(
                [$"Validation failed: {ex.Message}"]);
        }
    }

    /// <inheritdoc />
    public async Task<BackupResult> CreateBackupsAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        string? backupDirectory = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateFiles);

        try
        {

            if (templateFiles.Count == 0)
            {
                return BackupResult.Success([], "", TimeSpan.Zero);
            }

            var startTime = DateTime.UtcNow;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var actualBackupDirectory = backupDirectory ??
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(templateFiles[0].FullPath)!, $"backup_{timestamp}");

            _fileSystemService.CreateDirectory(actualBackupDirectory);

            var backupDetails = new List<BackupDetail>();
            var errors = new List<BackupError>();
            var failedCount = 0;

            foreach (var templateFile in templateFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var fileName = System.IO.Path.GetFileName(templateFile.FullPath);
                    var backupPath = System.IO.Path.Combine(actualBackupDirectory, fileName);

                    _fileSystemService.CopyFile(templateFile.FullPath, backupPath);

                    backupDetails.Add(new BackupDetail
                    {
                        SourcePath = templateFile.FullPath,
                        BackupPath = backupPath,
                        SizeBytes = templateFile.SizeInBytes
                    });

                    _logger.LogDebug("Created backup: {BackupPath}", backupPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to backup file: {FilePath}", templateFile.FullPath);
                    failedCount++;
                    errors.Add(new BackupError
                    {
                        SourcePath = templateFile.FullPath,
                        BackupPath = System.IO.Path.Combine(actualBackupDirectory, System.IO.Path.GetFileName(templateFile.FullPath)),
                        Message = ex.Message,
                        ExceptionType = ex.GetType().Name
                    });
                }
            }

            var duration = DateTime.UtcNow - startTime;

            if (failedCount == 0)
            {
                return BackupResult.Success(backupDetails, actualBackupDirectory, duration);
            }
            else
            {
                return BackupResult.WithFailures(backupDetails, actualBackupDirectory, duration, failedCount, errors);
            }
        }
        catch (Exception ex)
        {
            var errorResult = await _errorHandler.HandleExceptionAsync(ex, "backup creation");
            return BackupResult.WithFailures(
                [],
                "",
                TimeSpan.Zero,
                1,
                [new BackupError { SourcePath = "", BackupPath = "", Message = errorResult?.Message ?? ex.Message }]);
        }
    }

    private Task<IReadOnlyList<TemplateFile>> DiscoverTemplateFilesAsync(string folderPath, CancellationToken cancellationToken)
    {
        var files = _fileSystemService.EnumerateFiles(folderPath, "*.docx", SearchOption.AllDirectories);
        var templateFiles = new List<TemplateFile>();

        foreach (var filePath in files)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var fileName = _fileSystemService.GetFileName(filePath);
                var relativePath = System.IO.Path.GetRelativePath(folderPath, filePath);
                var fileSize = _fileSystemService.GetFileSize(filePath);
                var lastModified = _fileSystemService.GetLastWriteTime(filePath);

                var templateFile = new TemplateFile
                {
                    FullPath = filePath,
                    RelativePath = relativePath,
                    FileName = fileName,
                    SizeInBytes = fileSize,
                    LastModified = lastModified
                };

                templateFiles.Add(templateFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process template file: {FilePath}", filePath);
            }
        }

        return Task.FromResult<IReadOnlyList<TemplateFile>>(templateFiles);
    }

    private async Task<string> CreateFileBackupAsync(string filePath, CancellationToken cancellationToken)
    {
        var backupPath = await _fileSystemService.CreateBackupAsync(filePath, cancellationToken);
        _logger.LogDebug("Created backup: {BackupPath}", backupPath);
        return backupPath;
    }

    private async Task<int> ProcessDocumentReplacementsAsync(
        string filePath,
        ReplacementMap replacementMap,
        CancellationToken cancellationToken)
    {
        // Create logger for the processor
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var processorLogger = loggerFactory.CreateLogger<ReplaceDocumentPartProcessor>();

        // Create replace processor with required dependencies
        var replaceProcessor = new ReplaceDocumentPartProcessor(
            replacementMap,
            ProcessParagraphReplacements,
            processorLogger);

        // Use DocumentTraverser to traverse all document parts uniformly
        // This will now process body, headers, AND footers (fixes the bug!)
        await _documentTraverser.TraverseDocumentAsync(
            filePath,
            isReadOnly: false,
            replaceProcessor,
            cancellationToken);

        var totalReplacements = replaceProcessor.TotalReplacements;
        _logger.LogDebug("Replaced {Count} placeholders in {FilePath}", totalReplacements, filePath);
        
        return totalReplacements;
    }

    private int ProcessParagraphReplacements(W.Paragraph paragraph, ReplacementMap replacementMap, OpenXmlPart documentPart)
    {
        var replacementCount = 0;
        
        // Use unified processor to reconstruct text and find placeholders
        var fullText = _processor.ReconstructParagraphText(paragraph);
        if (string.IsNullOrWhiteSpace(fullText))
            return 0;

        var matches = _processor.FindAllPlaceholders(fullText, documentPart.Uri?.ToString() ?? "document", "paragraph");
        
        // Handle image placeholders first (they take precedence)
        var imageMatches = matches.Where(m => m.Type == PlaceholderType.Image).ToList();
        if (imageMatches.Count > 0)
        {
            foreach (var match in imageMatches)
            {
                // Convert back to regex match for compatibility with existing image replacement logic
                var regexMatch = ImagePlaceholderPattern.Match(match.FullMatch);
                if (TryReplaceImagePlaceholder(paragraph, regexMatch, replacementMap, documentPart))
                {
                    replacementCount++;
                }
            }
            return replacementCount;
        }

        // Handle text placeholders using unified logic
        var textMatches = matches.Where(m => m.Type == PlaceholderType.Text).ToList();
        if (textMatches.Count > 0)
        {
            // Build text element map for coordinated replacement
            var textElements = _processor.BuildTextElementMap(paragraph);
            
            // Process placeholders in reverse order to maintain text positions
            var sortedMatches = textMatches.OrderByDescending(m => m.StartIndex).ToList();
            
            foreach (var match in sortedMatches)
            {
                if (replacementMap.Mappings.TryGetValue(match.PlaceholderName, out var replacement))
                {
                    // Use unified processor for replacement across elements
                    if (_processor.ReplaceTextAcrossElements(textElements, match.StartIndex, match.Length, replacement))
                    {
                        replacementCount++;
                    }
                }
            }
        }
        else
        {
            // Fallback: Process individual text elements for placeholders that might not be detected
            // due to complex formatting within the placeholder text
            var runs = paragraph.Descendants<W.Run>().ToList();
            foreach (var run in runs)
            {
                var textElements = run.Descendants<W.Text>().ToList();
                foreach (var textElement in textElements)
                {
                    var originalText = textElement.Text;
                    var newText = PlaceholderPattern.Replace(originalText, match =>
                    {
                        var placeholderName = match.Groups[1].Value.Trim();
                        if (replacementMap.Mappings.TryGetValue(placeholderName, out var replacement))
                        {
                            replacementCount++;
                            return replacement;
                        }
                        return match.Value; // Keep original if no replacement found
                    });

                    if (newText != originalText)
                    {
                        textElement.Text = newText;
                    }
                }
            }
        }

        return replacementCount;
    }


    private bool TryReplaceImagePlaceholder(W.Paragraph paragraph, Match match, ReplacementMap replacementMap, OpenXmlPart documentPart)
    {
        try
        {
            var imageName = match.Groups[1].Value;
            var width = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var height = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            
            // Check if we have a mapping for this image placeholder
            if (!replacementMap.Mappings.TryGetValue(imageName, out var imagePath) || 
                string.IsNullOrWhiteSpace(imagePath) || 
                !System.IO.File.Exists(imagePath))
            {
                _logger.LogWarning("Image file not found for placeholder {ImageName}: {ImagePath}", imageName, imagePath);
                return false;
            }

            // Get image information
            var imageInfo = _imageProcessor.GetImageInfo(imagePath);
            
            // Calculate display dimensions while preserving aspect ratio
            var (displayWidth, displayHeight) = AspectRatioCalculator.CalculateDisplayDimensions(
                imageInfo.Width, imageInfo.Height, width, height);

            // Convert to EMUs
            var widthEmus = UnitConverter.PixelsToEmus(displayWidth);
            var heightEmus = UnitConverter.PixelsToEmus(displayHeight);

            // Preserve existing paragraph properties (including alignment)
            var existingParagraphProperties = paragraph.GetFirstChild<W.ParagraphProperties>()?.CloneNode(true) as W.ParagraphProperties;
            
            // Clear all content from the paragraph
            paragraph.RemoveAllChildren();
            
            // Restore paragraph properties if they existed
            if (existingParagraphProperties != null)
            {
                paragraph.PrependChild(existingParagraphProperties);
            }
            
            // Create a new run with the image
            var imageRun = CreateImageRun(documentPart, imagePath, widthEmus, heightEmus);
            paragraph.AppendChild(imageRun);

            _logger.LogDebug("Replaced image placeholder {ImageName} with {ImagePath} ({Width}x{Height})", 
                imageName, imagePath, displayWidth, displayHeight);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replace image placeholder {Match}", match.Value);
            return false;
        }
    }

    private W.Run CreateImageRun(OpenXmlPart documentPart, string imagePath, long widthEmus, long heightEmus)
    {
        // Read image data
        var imageBytes = System.IO.File.ReadAllBytes(imagePath);

        // Determine the correct image part type
        var imagePartType = ImageTypeDetector.GetImagePartContentType(imagePath) switch
        {
            "image/png" => ImagePartType.Png,
            "image/jpeg" => ImagePartType.Jpeg,
            "image/gif" => ImagePartType.Gif,
            "image/bmp" => ImagePartType.Bmp,
            _ => ImagePartType.Png
        };

        // Add image part to the appropriate document part
        // Each part type (MainDocumentPart, HeaderPart, FooterPart) supports AddImagePart
        ImagePart imagePart = documentPart switch
        {
            MainDocumentPart mainPart => mainPart.AddImagePart(imagePartType),
            HeaderPart headerPart => headerPart.AddImagePart(imagePartType),
            FooterPart footerPart => footerPart.AddImagePart(imagePartType),
            _ => throw new NotSupportedException($"Document part type {documentPart.GetType().Name} does not support image parts")
        };

        using (var stream = new MemoryStream(imageBytes))
        {
            imagePart.FeedData(stream);
        }

        // Get relationship ID
        var relationshipId = documentPart.GetIdOfPart(imagePart);

        // Create the Drawing element
        var drawing = CreateImageDrawing(relationshipId, widthEmus, heightEmus);

        // Create and return the run
        var run = new W.Run();
        run.AppendChild(drawing);
        
        return run;
    }

    private W.Drawing CreateImageDrawing(string relationshipId, long widthEmus, long heightEmus)
    {
        var drawing = new W.Drawing(
            new DW.Inline(
                new DW.Extent() { Cx = widthEmus, Cy = heightEmus },
                new DW.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties() { Id = (uint)new Random().Next(1, 999999), Name = "Picture" },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks() { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties() { Id = 0U, Name = "Image" },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip() { Embed = relationshipId },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset() { X = 0L, Y = 0L },
                                    new A.Extents() { Cx = widthEmus, Cy = heightEmus }),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
                            )
                        )
                    ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                )
            )
        );
        
        return drawing;
    }

    private Task<FileReplacementPreview> CreateFilePreviewAsync(
        string filePath,
        ReplacementMap replacementMap,
        CancellationToken cancellationToken)
    {
        var replacementDetails = new List<ReplacementDetail>();

        try
        {
            using var wordDocument = WordprocessingDocument.Open(filePath, false); // Read-only
            var body = wordDocument.MainDocumentPart?.Document?.Body;

            if (body != null)
            {
                var textElements = body.Descendants<W.Text>().ToList();
                var placeholderCounts = new Dictionary<string, int>();

                foreach (var textElement in textElements)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var matches = PlaceholderPattern.Matches(textElement.Text);
                    foreach (Match match in matches)
                    {
                        var placeholderName = match.Groups[1].Value.Trim();
                        placeholderCounts[placeholderName] = placeholderCounts.GetValueOrDefault(placeholderName, 0) + 1;
                    }
                }

                foreach (var kvp in placeholderCounts)
                {
                    var placeholderName = kvp.Key;
                    var count = kvp.Value;
                    var hasReplacement = replacementMap.Mappings.TryGetValue(placeholderName, out var replacement);

                    replacementDetails.Add(new ReplacementDetail
                    {
                        PlaceholderName = placeholderName,
                        CurrentValue = $"{{{{{placeholderName}}}}}",
                        NewValue = replacement ?? string.Empty,
                        OccurrenceCount = count
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to preview file: {FilePath}", filePath);
            return Task.FromResult(new FileReplacementPreview
            {
                FilePath = filePath,
                ReplacementCount = 0,
                CanProcess = false,
                ErrorMessage = ex.Message,
                ReplacementDetails = []
            });
        }

        var totalReplacements = replacementDetails.Where(r => !string.IsNullOrEmpty(r.NewValue)).Sum(r => r.OccurrenceCount);

        return Task.FromResult(new FileReplacementPreview
        {
            FilePath = filePath,
            ReplacementCount = totalReplacements,
            CanProcess = true,
            ReplacementDetails = replacementDetails
        });
    }

    /// <inheritdoc />
    public async Task<ReplaceResult> ReplaceInTemplateAsync(
        TemplateFile templateFile,
        ReplacementMap replacementMap,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateFile);
        ArgumentNullException.ThrowIfNull(replacementMap);

        var startTime = DateTime.UtcNow;

        try
        {
            var replacementCount = await ProcessDocumentReplacementsAsync(templateFile.FullPath, replacementMap, cancellationToken);

            // Handle file prefix renaming if SOUBOR_PREFIX was provided
            var finalFilePath = templateFile.FullPath;
            if (replacementMap.Mappings.TryGetValue(Placeholder.FilePrefixPlaceholder, out var prefix) && 
                !string.IsNullOrWhiteSpace(prefix))
            {
                finalFilePath = await ApplyFilePrefixAsync(templateFile.FullPath, prefix, cancellationToken);
            }

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            var fileResult = FileReplaceResult.Success(
                finalFilePath,
                replacementCount,
                processingDuration: duration);

            return ReplaceResult.Success(
                [fileResult],
                duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replace placeholders in template {TemplatePath}", templateFile.FullPath);

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            var fileResult = FileReplaceResult.Failure(
                templateFile.FullPath,
                ex.Message,
                duration);

            return ReplaceResult.Success(
                [fileResult],
                duration);
        }
    }

    /// <summary>
    /// Applies a file prefix to a template file by renaming it
    /// </summary>
    /// <param name="originalPath">The original file path</param>
    /// <param name="prefix">The prefix to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The new file path after renaming</returns>
    private async Task<string> ApplyFilePrefixAsync(string originalPath, string prefix, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Make method async
        
        var directory = System.IO.Path.GetDirectoryName(originalPath) ?? throw new InvalidOperationException("Could not determine directory");
        var fileName = System.IO.Path.GetFileName(originalPath);
        var extension = System.IO.Path.GetExtension(fileName);
        var fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);
        
        // Sanitize prefix for file system compatibility
        var sanitizedPrefix = SanitizePrefix(prefix);
        if (string.IsNullOrWhiteSpace(sanitizedPrefix))
        {
            _logger.LogDebug("Empty prefix after sanitization, keeping original filename: {OriginalPath}", originalPath);
            return originalPath;
        }
        
        // Create new filename with prefix
        var newFileName = $"{sanitizedPrefix}_{fileNameWithoutExtension}{extension}";
        var newFilePath = System.IO.Path.Combine(directory, newFileName);
        
        // Handle file conflicts by adding numeric suffix
        newFilePath = GetUniqueFilePath(newFilePath);
        
        try
        {
            _fileSystemService.MoveFile(originalPath, newFilePath);
            _logger.LogDebug("Applied file prefix: {OriginalPath} -> {NewPath}", originalPath, newFilePath);
            return newFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply file prefix, keeping original name: {OriginalPath}", originalPath);
            return originalPath; // Fall back to original path if rename fails
        }
    }

    /// <summary>
    /// Sanitizes a prefix string for file system compatibility
    /// </summary>
    /// <param name="prefix">The raw prefix</param>
    /// <returns>Sanitized prefix safe for file names</returns>
    private static string SanitizePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return string.Empty;
        
        // Remove or replace invalid file name characters
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        var sanitized = prefix;
        
        foreach (var invalidChar in invalidChars)
        {
            sanitized = sanitized.Replace(invalidChar, '_');
        }
        
        // Additional sanitization for common problematic characters
        sanitized = sanitized
            .Replace(":", "_")
            .Replace("*", "_")
            .Replace("?", "_")
            .Replace("\"", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace("|", "_");
        
        // Trim whitespace and limit length
        sanitized = sanitized.Trim();
        if (sanitized.Length > 50) // Reasonable limit to avoid long paths
        {
            sanitized = sanitized.Substring(0, 50).Trim();
        }
        
        return sanitized;
    }

    /// <summary>
    /// Gets a unique file path by adding numeric suffix if file already exists
    /// </summary>
    /// <param name="originalPath">The desired file path</param>
    /// <returns>Unique file path</returns>
    private string GetUniqueFilePath(string originalPath)
    {
        if (!_fileSystemService.FileExists(originalPath))
            return originalPath;
        
        var directory = System.IO.Path.GetDirectoryName(originalPath) ?? throw new InvalidOperationException("Could not determine directory");
        var fileName = System.IO.Path.GetFileNameWithoutExtension(originalPath);
        var extension = System.IO.Path.GetExtension(originalPath);
        
        var counter = 1;
        string newPath;
        
        do
        {
            var newFileName = $"{fileName}({counter}){extension}";
            newPath = System.IO.Path.Combine(directory, newFileName);
            counter++;
        } 
        while (_fileSystemService.FileExists(newPath) && counter < 1000); // Prevent infinite loop
        
        return newPath;
    }
}
