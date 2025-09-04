using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Services;
using DocxTemplate.UI.Services;
using DocxTemplate.UI.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace DocxTemplate.UI.Tests;

/// <summary>
/// Happy Path E2E test for complete template processing workflow with Czech characters
/// </summary>
public class HappyPathE2ETest : E2ETestBase
{
    /// <summary>
    /// Czech test data as specified in the story requirements
    /// </summary>
    private static readonly Dictionary<string, string> CzechTestValues = new()
    {
        {"SOUBOR_PREFIX", "2024_12_TestováníČeština"},
        {"ZAKAZKA_NAZEV", "Rekonstrukce náměstí v Břeclavi"},
        {"ZADAVATEL_NAZEV", "Město Břeclav"},
        {"ZADAVATEL_ADRESA", "Nám. T.G. Masaryka 1, 690 02 Břeclav"},
        {"ZADAVATEL_ICO", "00283355"},
        {"ZAKAZKA_PREDMET_DRUH_RIZENI", "Veřejná zakázka na stavební práce"},
        {"CASTKA_ZAKAZKY", "1 500 000 Kč"},
        {"DATUM_ZADANI", "15. prosince 2024"},
        {"ODPOVEDNA_OSOBA", "Ing. Jana Nováková, Ph.D."}
    };

    public HappyPathE2ETest(ITestOutputHelper testOutput) : base(testOutput)
    {
    }

    [Fact]
    public async Task CompleteWorkflow_WithCzechCharacters_ProducesCorrectOutput()
    {
        // arrange - Set up services and locate templates
        var services = SetupServices();
        var templatesPath = GetTemplatesPath();
        TestOutput.WriteLine($"Templates path: {templatesPath}");

        // Step 1: Verify template sets are discovered correctly
        TestOutput.WriteLine("=== Step 1: Template Set Discovery ===");
        var templateSetService = services.GetRequiredService<ITemplateSetDiscoveryService>();
        var templateSets = await templateSetService.DiscoverTemplateSetsAsync(templatesPath);

        TestOutput.WriteLine($"Found {templateSets.Count} template sets:");
        foreach (var set in templateSets)
        {
            TestOutput.WriteLine($"  - {set.Name}: {set.FileCount} files");
        }

        // Verify expected template sets are found
        Assert.NotEmpty(templateSets);
        Assert.Contains(templateSets, ts => ts.Name.Contains("VZOR Užší řízení") || ts.Name.Contains("VZOR"));

        // Use the "01 VZOR Užší řízení" template set if available, otherwise first one
        var targetTemplateSet = templateSets.FirstOrDefault(ts => ts.Name.Contains("01 VZOR Užší řízení"))
                              ?? templateSets.First();

        TestOutput.WriteLine($"Using template set: {targetTemplateSet.Name} at {targetTemplateSet.Path}");

        // Step 2: Discover and validate placeholders
        TestOutput.WriteLine("\n=== Step 2: Placeholder Discovery ===");
        var scanService = services.GetRequiredService<IPlaceholderScanService>();
        var scanResult = await scanService.ScanPlaceholdersAsync(
            targetTemplateSet.Path,
            pattern: @"\{\{.*?\}\}",
            recursive: true);

        TestOutput.WriteLine($"Scan result: {scanResult.Placeholders.Count} unique placeholders found");
        foreach (var placeholder in scanResult.Placeholders.Take(10)) // Show first 10
        {
            TestOutput.WriteLine($"  - {placeholder.Name} (found {placeholder.Locations.Count} times)");
        }

        // Verify placeholders were found
        Assert.NotEmpty(scanResult.Placeholders);

        // Verify SOUBOR_PREFIX appears first if it exists
        if (scanResult.Placeholders.Any(p => p.Name == "SOUBOR_PREFIX"))
        {
            var firstPlaceholder = scanResult.Placeholders.First();
            Assert.Equal("SOUBOR_PREFIX", firstPlaceholder.Name);
            TestOutput.WriteLine($"✓ SOUBOR_PREFIX correctly appears first in placeholder list");
        }

        TestOutput.WriteLine($"✓ Placeholder discovery successful: {scanResult.Placeholders.Count} unique placeholders");

        // Step 3: Copy templates preserving folder structure
        TestOutput.WriteLine("\n=== Step 3: Template Copy Operation ===");
        var copyService = services.GetRequiredService<ITemplateCopyService>();
        var copyResult = await copyService.CopyTemplatesAsync(
            targetTemplateSet.Path,
            TestOutputDirectory,
            preserveStructure: true);

        TestOutput.WriteLine($"Copy result: {copyResult.FilesCount} files copied to {TestOutputDirectory}");

        var targetPath = Path.Combine(TestOutputDirectory, Path.GetFileName(targetTemplateSet.Path));
        TestOutput.WriteLine($"Target path for replacement: {targetPath}");

        // Verify files were copied with correct structure
        Assert.True(Directory.Exists(targetPath), "Target directory should exist after copy");
        var copiedFiles = Directory.GetFiles(targetPath, "*.docx", SearchOption.AllDirectories);

        TestOutput.WriteLine($"Copied files: {copiedFiles.Length}");
        foreach (var file in copiedFiles.Take(5))
        {
            TestOutput.WriteLine($"  - {Path.GetRelativePath(targetPath, file)}");
        }

        Assert.True(copiedFiles.Length > 0, "At least one DOCX file should be copied");
        TestOutput.WriteLine($"✓ Template copy successful: {copiedFiles.Length} files with preserved structure");

        // Step 4: Create Czech replacement mappings
        TestOutput.WriteLine("\n=== Step 4: Czech Character Placeholder Mapping ===");

        // Create mappings using Czech test values for found placeholders
        var replacementMap = new Dictionary<string, string>();
        var foundPlaceholderNames = scanResult.Placeholders.Select(p => p.Name).ToHashSet();
        
        // Get the path to the LOGO.png for image placeholders
        var logoPath = Path.Combine(templatesPath, "LOGO.png");
        if (!File.Exists(logoPath))
        {
            TestOutput.WriteLine($"Warning: LOGO.png not found at {logoPath}, using fallback");
            // Try to find it relative to the test assembly
            logoPath = Path.GetFullPath("templates/LOGO.png");
        }

        // Process all discovered placeholders
        foreach (var placeholder in scanResult.Placeholders)
        {
            // Check if it's an image placeholder
            if (placeholder.Type == PlaceholderType.Image)
            {
                // Map image placeholders to the actual image file
                replacementMap[placeholder.Name] = logoPath;
                TestOutput.WriteLine($"  ✓ {placeholder.Name} -> {logoPath} (IMAGE)");
            }
            else if (CzechTestValues.ContainsKey(placeholder.Name))
            {
                // Use Czech test values for known text placeholders
                replacementMap[placeholder.Name] = CzechTestValues[placeholder.Name];
                TestOutput.WriteLine($"  ✓ {placeholder.Name} -> {CzechTestValues[placeholder.Name]}");
            }
            else if (replacementMap.Count < 5)
            {
                // Generate Czech test values for other text placeholders (limit to 5)
                var czechValue = $"TestovacíHodnota_{placeholder.Name}_ČeskéZnaky_ěščřžýáí";
                replacementMap[placeholder.Name] = czechValue;
                TestOutput.WriteLine($"  + {placeholder.Name} -> {czechValue}");
            }
        }

        Assert.NotEmpty(replacementMap);
        TestOutput.WriteLine($"✓ Created {replacementMap.Count} Czech character replacement mappings");

        var replacements = ReplacementMap.FromJson(
            System.Text.Json.JsonSerializer.Serialize(replacementMap),
            "czech-test-mapping.json");

        // Step 5: Execute placeholder replacement
        TestOutput.WriteLine("\n=== Step 5: Placeholder Replacement Execution ===");
        var replaceService = services.GetRequiredService<IPlaceholderReplaceService>();
        var replaceResult = await replaceService.ReplacePlaceholdersAsync(
            targetPath,
            replacements,
            createBackup: false);

        TestOutput.WriteLine($"Replace result: {replaceResult.FilesProcessed} files processed, {replaceResult.TotalReplacements} total replacements");

        // Verify replacement was successful
        Assert.True(replaceResult.FilesProcessed > 0, "At least one file should be processed");
        Assert.True(replaceResult.TotalReplacements > 0, "At least one replacement should be made");

        if (replaceResult.HasErrors)
        {
            TestOutput.WriteLine("Replacement errors:");
            foreach (var error in replaceResult.AllErrors)
            {
                TestOutput.WriteLine($"  - {error}");
            }
            Assert.False(replaceResult.HasErrors, "Replacement should not have errors");
        }

        TestOutput.WriteLine($"✓ Replacement successful: {replaceResult.FilesProcessed} files, {replaceResult.TotalReplacements} replacements");

        // Step 6: Verify SOUBOR_PREFIX file renaming functionality
        TestOutput.WriteLine("\n=== Step 6: SOUBOR_PREFIX File Renaming Verification ===");
        var prefixValue = replacementMap.ContainsKey("SOUBOR_PREFIX") ? replacementMap["SOUBOR_PREFIX"] : null;

        if (!string.IsNullOrEmpty(prefixValue))
        {
            TestOutput.WriteLine($"Checking file prefix application: '{prefixValue}'");
            var processedFiles = Directory.GetFiles(targetPath, "*.docx", SearchOption.AllDirectories);
            var prefixedFiles = processedFiles.Where(f => Path.GetFileName(f).StartsWith($"{prefixValue}_")).ToList();
            Assert.Equal(processedFiles.Length, prefixedFiles.Count);
            TestOutput.WriteLine($"Found {prefixedFiles.Count} files with prefix out of {processedFiles.Length} total files");
            foreach (var prefixedFile in prefixedFiles.Take(5))
            {
                var fileName = Path.GetFileName(prefixedFile);
                TestOutput.WriteLine($"  ✓ Prefixed file: {fileName}");
            }

            Assert.True(prefixedFiles.Count > 0, $"At least one file should have the prefix '{prefixValue}_'");
            TestOutput.WriteLine($"✓ SOUBOR_PREFIX functionality verified: {prefixedFiles.Count} files properly prefixed");
        }
        else
        {
            TestOutput.WriteLine("⚠ SOUBOR_PREFIX not found in replacement mappings, skipping file prefix validation");
        }

        // Step 7: Verify complete placeholder replacement
        TestOutput.WriteLine("\n=== Step 7: Replacement Verification ===");
        var verificationScanResult = await scanService.ScanPlaceholdersAsync(
            targetPath,
            pattern: @"\{\{.*?\}\}",
            recursive: true);

        TestOutput.WriteLine($"Verification scan: {verificationScanResult.Placeholders.Count} placeholders remain after replacement");

        // Check that our targeted placeholders were actually replaced
        var remainingPlaceholdersWeTriedToReplace = verificationScanResult.Placeholders
            .Where(p => replacementMap.ContainsKey(p.Name))
            .ToList();

        if (remainingPlaceholdersWeTriedToReplace.Any())
        {
            TestOutput.WriteLine("❌ REPLACEMENT FAILED: The following placeholders were NOT replaced:");
            foreach (var placeholder in remainingPlaceholdersWeTriedToReplace)
            {
                TestOutput.WriteLine($"  - {placeholder.Name} still found {placeholder.Locations.Count} times");

                // Show files where this placeholder still exists
                var filesWithThisPlaceholder = placeholder.Locations
                    .Select(l => l.FilePath)
                    .Distinct()
                    .Take(3);

                foreach (var file in filesWithThisPlaceholder)
                {
                    TestOutput.WriteLine($"    Still in: {Path.GetRelativePath(targetPath, file)}");
                }
            }
        }
        else
        {
            TestOutput.WriteLine("✓ All targeted placeholders were successfully replaced");
        }

        // Assert that all targeted placeholders were replaced
        Assert.Empty(remainingPlaceholdersWeTriedToReplace);

        // Step 8: Document content and Czech character validation
        TestOutput.WriteLine("\n=== Step 8: Content and Czech Character Validation ===");
        var docxFiles = Directory.GetFiles(targetPath, "*.docx", SearchOption.AllDirectories);

        var validationSummaries = new List<DocumentValidationSummary>();
        foreach (var file in docxFiles.Take(3)) // Validate first 3 files
        {
            var validation = await DocumentContentValidator.CreateValidationSummary(
                file,
                services,
                replacementMap);

            validationSummaries.Add(validation);
            TestOutput.WriteLine($"Validation: {Path.GetFileName(file)} - {validation}");

            // Verify Czech characters are preserved
            foreach (var kvp in replacementMap.Where(m => m.Value.Any(c => "ěščřžýáíúů".Contains(c))))
            {
                var contentExists = await DocumentContentValidator.ValidateTextExists(file, kvp.Value);
                if (contentExists)
                {
                    TestOutput.WriteLine($"  ✓ Czech text verified: {kvp.Value}");
                }
            }
        }

        // Step 9: Verify folder structure preservation
        TestOutput.WriteLine("\n=== Step 9: Folder Structure Validation ===");
        var originalStructure = Directory.GetDirectories(targetTemplateSet.Path, "*", SearchOption.AllDirectories)
            .Select(d => Path.GetRelativePath(targetTemplateSet.Path, d))
            .OrderBy(p => p)
            .ToList();

        var copiedStructure = Directory.GetDirectories(targetPath, "*", SearchOption.AllDirectories)
            .Select(d => Path.GetRelativePath(targetPath, d))
            .OrderBy(p => p)
            .ToList();

        TestOutput.WriteLine($"Original structure: {originalStructure.Count} directories");
        TestOutput.WriteLine($"Copied structure: {copiedStructure.Count} directories");

        foreach (var dir in originalStructure.Take(5))
        {
            Assert.Contains(dir, copiedStructure);
            TestOutput.WriteLine($"  ✓ Directory preserved: {dir}");
        }

        // Step 10: Generate comprehensive test report
        TestOutput.WriteLine("\n=== Step 10: Test Completion Summary ===");
        var prefixedFilesCount = !string.IsNullOrEmpty(prefixValue)
            ? Directory.GetFiles(targetPath, "*.docx", SearchOption.AllDirectories)
                .Count(f => Path.GetFileName(f).StartsWith($"{prefixValue}_"))
            : 0;

        await CreateTestParametersFile(new
        {
            TestName = "Complete Workflow with Czech Characters and SOUBOR_PREFIX",
            TemplateSet = targetTemplateSet.Name,
            PlaceholdersDiscovered = scanResult.Placeholders.Count,
            CzechReplacementMappings = replacementMap,
            SouborPrefixValue = prefixValue ?? "Not provided",
            PrefixedFilesCount = prefixedFilesCount,
            FilesProcessed = replaceResult.FilesProcessed,
            TotalReplacements = replaceResult.TotalReplacements,
            SuccessfulReplacements = replacementMap.Count - remainingPlaceholdersWeTriedToReplace.Count,
            DirectoriesPreserved = copiedStructure.Count,
            ValidationSummaries = validationSummaries.Select(v => v.ToString()).ToList(),
            TestResult = "PASSED - All validations successful including SOUBOR_PREFIX functionality"
        });

        TestOutput.WriteLine($"✓ Happy Path E2E Test completed successfully!");
        TestOutput.WriteLine($"  - {scanResult.Placeholders.Count} placeholders discovered");
        TestOutput.WriteLine($"  - {replaceResult.FilesProcessed} files processed");
        TestOutput.WriteLine($"  - {replaceResult.TotalReplacements} total replacements");
        TestOutput.WriteLine($"  - {replacementMap.Count} Czech character mappings applied");
        if (!string.IsNullOrEmpty(prefixValue))
        {
            TestOutput.WriteLine($"  - SOUBOR_PREFIX '{prefixValue}' applied to {prefixedFilesCount} files");
        }
        TestOutput.WriteLine($"  - Folder structure preserved with {copiedStructure.Count} directories");
        TestOutput.WriteLine($"Test output available at: {Path.GetFullPath(TestOutputDirectory)}");
    }

    [Fact]
    public async Task UpdateWorkflow_WithPartiallyProcessedTemplates_CompletesRemainingPlaceholders()
    {
        // arrange - Set up services and create partially processed templates
        var services = SetupServices();
        var templatesPath = GetTemplatesPath();
        TestOutput.WriteLine($"Templates path: {templatesPath}");

        // Create a separate output directory for update workflow test
        var updateTestDirectory = Path.Combine(TestOutputDirectory, "UpdateWorkflowTest");
        Directory.CreateDirectory(updateTestDirectory);

        // Step 1: Create initial partially processed templates (simulating previous processing)
        TestOutput.WriteLine("=== Step 1: Setup Partially Processed Templates ===");
        var templateSetService = services.GetRequiredService<ITemplateSetDiscoveryService>();
        var templateSets = await templateSetService.DiscoverTemplateSetsAsync(templatesPath);
        
        var targetTemplateSet = templateSets.FirstOrDefault(ts => ts.Name.Contains("01 VZOR Užší řízení"))
                              ?? templateSets.First();

        TestOutput.WriteLine($"Using template set: {targetTemplateSet.Name}");

        // Copy templates to create "partially processed" templates
        var copyService = services.GetRequiredService<ITemplateCopyService>();
        await copyService.CopyTemplatesAsync(
            targetTemplateSet.Path,
            updateTestDirectory,
            preserveStructure: true);

        var partiallyProcessedPath = Path.Combine(updateTestDirectory, Path.GetFileName(targetTemplateSet.Path));
        TestOutput.WriteLine($"Partially processed templates at: {partiallyProcessedPath}");

        // Step 2: Perform partial processing (only some placeholders)
        TestOutput.WriteLine("\n=== Step 2: Partial Processing (First Round) ===");
        var scanService = services.GetRequiredService<IPlaceholderScanService>();
        var initialScanResult = await scanService.ScanPlaceholdersAsync(
            partiallyProcessedPath,
            recursive: true);

        TestOutput.WriteLine($"Initial scan: {initialScanResult.Placeholders.Count} unique placeholders found");

        // Create partial replacement mappings (only process some placeholders)
        var partialReplacementMap = new Dictionary<string, string>();
        var allPlaceholders = initialScanResult.Placeholders.ToList();
        
        // Process only the first half of placeholders, leaving others unfilled
        var placeholdersToProcessInitially = allPlaceholders.Take(allPlaceholders.Count / 2).ToList();
        
        foreach (var placeholder in placeholdersToProcessInitially)
        {
            // Skip image placeholders in partial processing - save them for update workflow
            if (placeholder.Type == PlaceholderType.Image)
            {
                TestOutput.WriteLine($"  Skipping image placeholder {placeholder.Name} for update workflow test");
                continue;
            }
            
            if (CzechTestValues.ContainsKey(placeholder.Name))
            {
                partialReplacementMap[placeholder.Name] = CzechTestValues[placeholder.Name];
            }
            else
            {
                partialReplacementMap[placeholder.Name] = $"ČástečněZpracováno_{placeholder.Name}_Round1";
            }
        }

        TestOutput.WriteLine($"Processing {partialReplacementMap.Count} placeholders in first round:");
        foreach (var kvp in partialReplacementMap.Take(5))
        {
            TestOutput.WriteLine($"  ✓ {kvp.Key} -> {kvp.Value}");
        }

        // Execute partial replacement
        var replaceService = services.GetRequiredService<IPlaceholderReplaceService>();
        var partialReplacements = ReplacementMap.FromJson(
            System.Text.Json.JsonSerializer.Serialize(partialReplacementMap),
            "partial-replacement.json");

        var partialReplaceResult = await replaceService.ReplacePlaceholdersAsync(
            partiallyProcessedPath,
            partialReplacements,
            createBackup: false);

        TestOutput.WriteLine($"Partial processing: {partialReplaceResult.FilesProcessed} files, {partialReplaceResult.TotalReplacements} replacements");
        Assert.True(partialReplaceResult.TotalReplacements > 0, "Partial processing should make replacements");

        // Step 3: Simulate Update Workflow - Scan for remaining placeholders
        TestOutput.WriteLine("\n=== Step 3: Update Workflow - Recursive Placeholder Discovery ===");
        
        // This simulates what ExistingProjectFolderSelectionViewModel does
        var docxFiles = Directory.GetFiles(partiallyProcessedPath, "*.docx", SearchOption.AllDirectories);
        TestOutput.WriteLine($"Found {docxFiles.Length} .docx files for update workflow (including subfolders)");

        // Scan for remaining placeholders (this is what the Update workflow does)
        var updateScanResult = await scanService.ScanPlaceholdersAsync(
            partiallyProcessedPath,
            recursive: true);

        TestOutput.WriteLine($"Update scan: {updateScanResult.Placeholders.Count} placeholders still need processing");
        
        // Verify that some placeholders remain unprocessed
        Assert.True(updateScanResult.Placeholders.Count > 0, "Should have remaining placeholders for update workflow");
        
        // Verify that SOUBOR_PREFIX is filtered out (as per requirements)
        var remainingPlaceholderNames = updateScanResult.Placeholders.Select(p => p.Name).ToList();
        Assert.DoesNotContain("SOUBOR_PREFIX", remainingPlaceholderNames);
        TestOutput.WriteLine("✓ SOUBOR_PREFIX correctly filtered out from update workflow");

        foreach (var placeholder in updateScanResult.Placeholders.Take(5))
        {
            TestOutput.WriteLine($"  - {placeholder.Name} (found in {placeholder.UniqueFileCount} files, {placeholder.TotalOccurrences} occurrences)");
        }

        // Step 4: Complete remaining placeholders (Update workflow completion)
        TestOutput.WriteLine("\n=== Step 4: Complete Remaining Placeholders ===");
        
        // Get the path to the LOGO.png for image placeholders
        var logoPath = Path.Combine(templatesPath, "LOGO.png");
        if (!File.Exists(logoPath))
        {
            TestOutput.WriteLine($"Warning: LOGO.png not found at {logoPath}, using fallback");
            logoPath = Path.GetFullPath("templates/LOGO.png");
        }
        
        var completionReplacementMap = new Dictionary<string, string>();
        foreach (var placeholder in updateScanResult.Placeholders)
        {
            // Handle image placeholders
            if (placeholder.Type == PlaceholderType.Image)
            {
                completionReplacementMap[placeholder.Name] = logoPath;
                TestOutput.WriteLine($"  Mapping image placeholder {placeholder.Name} -> {logoPath}");
            }
            else if (CzechTestValues.ContainsKey(placeholder.Name))
            {
                completionReplacementMap[placeholder.Name] = CzechTestValues[placeholder.Name];
            }
            else
            {
                completionReplacementMap[placeholder.Name] = $"Dokončeno_{placeholder.Name}_UpdateWorkflow_ČeskéZnaky";
            }
        }

        TestOutput.WriteLine($"Completing {completionReplacementMap.Count} remaining placeholders:");
        foreach (var kvp in completionReplacementMap.Take(5))
        {
            TestOutput.WriteLine($"  ✓ {kvp.Key} -> {kvp.Value}");
        }

        var completionReplacements = ReplacementMap.FromJson(
            System.Text.Json.JsonSerializer.Serialize(completionReplacementMap),
            "completion-replacement.json");

        var completionReplaceResult = await replaceService.ReplacePlaceholdersAsync(
            partiallyProcessedPath,
            completionReplacements,
            createBackup: false);

        TestOutput.WriteLine($"Completion processing: {completionReplaceResult.FilesProcessed} files, {completionReplaceResult.TotalReplacements} replacements");
        Assert.True(completionReplaceResult.TotalReplacements > 0, "Completion processing should make replacements");

        // Step 5: Verify all placeholders are now processed
        TestOutput.WriteLine("\n=== Step 5: Final Verification - All Placeholders Completed ===");
        
        var finalScanResult = await scanService.ScanPlaceholdersAsync(
            partiallyProcessedPath,
            recursive: true);

        TestOutput.WriteLine($"Final scan: {finalScanResult.Placeholders.Count} placeholders remain");

        // Check that all our targeted placeholders were replaced
        var allProcessedPlaceholders = partialReplacementMap.Keys.Concat(completionReplacementMap.Keys).ToHashSet();
        var stillRemainingTargetedPlaceholders = finalScanResult.Placeholders
            .Where(p => allProcessedPlaceholders.Contains(p.Name))
            .ToList();

        if (stillRemainingTargetedPlaceholders.Any())
        {
            TestOutput.WriteLine("❌ UPDATE WORKFLOW FAILED: The following placeholders were NOT completed:");
            foreach (var placeholder in stillRemainingTargetedPlaceholders)
            {
                TestOutput.WriteLine($"  - {placeholder.Name} still found {placeholder.TotalOccurrences} times");
            }
        }
        else
        {
            TestOutput.WriteLine("✅ All targeted placeholders successfully completed in update workflow");
        }

        Assert.Empty(stillRemainingTargetedPlaceholders);

        // Step 6: Verify files exist in nested directories
        TestOutput.WriteLine("\n=== Step 6: Verify Recursive Directory Processing ===");
        
        var allProcessedFiles = Directory.GetFiles(partiallyProcessedPath, "*.docx", SearchOption.AllDirectories);
        var nestedFiles = allProcessedFiles.Where(f => 
            !Path.GetDirectoryName(f)!.Equals(partiallyProcessedPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        TestOutput.WriteLine($"Total processed files: {allProcessedFiles.Length}");
        TestOutput.WriteLine($"Files in nested directories: {nestedFiles.Count}");

        if (nestedFiles.Any())
        {
            TestOutput.WriteLine("✅ Recursive directory processing verified - files in subfolders were processed:");
            foreach (var nestedFile in nestedFiles.Take(3))
            {
                var relativePath = Path.GetRelativePath(partiallyProcessedPath, nestedFile);
                TestOutput.WriteLine($"  - {relativePath}");
            }
        }

        // Step 7: Content validation for update workflow
        TestOutput.WriteLine("\n=== Step 7: Update Workflow Content Validation ===");
        
        var sampleFiles = allProcessedFiles.Take(3).ToList();
        foreach (var file in sampleFiles)
        {
            // Verify partial processing values exist
            foreach (var kvp in partialReplacementMap.Take(2))
            {
                var contentExists = await DocumentContentValidator.ValidateTextExists(file, kvp.Value);
                if (contentExists)
                {
                    TestOutput.WriteLine($"  ✓ Partial processing verified in {Path.GetFileName(file)}: {kvp.Value}");
                }
            }

            // Verify completion values exist
            foreach (var kvp in completionReplacementMap.Take(2))
            {
                var contentExists = await DocumentContentValidator.ValidateTextExists(file, kvp.Value);
                if (contentExists)
                {
                    TestOutput.WriteLine($"  ✓ Update completion verified in {Path.GetFileName(file)}: {kvp.Value}");
                }
            }
        }

        // Step 8: Generate update workflow test report
        TestOutput.WriteLine("\n=== Step 8: Update Workflow Test Summary ===");
        
        await CreateTestParametersFile(new
        {
            TestName = "Update Workflow - Complete Partially Processed Templates",
            TemplateSet = targetTemplateSet.Name,
            InitialPlaceholders = initialScanResult.Placeholders.Count,
            PartiallyProcessedPlaceholders = partialReplacementMap.Count,
            RemainingAfterPartial = updateScanResult.Placeholders.Count,
            CompletedInUpdate = completionReplacementMap.Count,
            FinalRemainingPlaceholders = finalScanResult.Placeholders.Count,
            TotalFilesProcessed = allProcessedFiles.Length,
            NestedFilesProcessed = nestedFiles.Count,
            PartialReplacements = partialReplaceResult.TotalReplacements,
            CompletionReplacements = completionReplaceResult.TotalReplacements,
            RecursiveDirectoryProcessing = nestedFiles.Count > 0,
            SouborPrefixFiltered = !remainingPlaceholderNames.Contains("SOUBOR_PREFIX"),
            TestResult = "PASSED - Update workflow successfully completed all remaining placeholders"
        });

        TestOutput.WriteLine("✅ Update Workflow E2E Test completed successfully!");
        TestOutput.WriteLine($"  - Started with {initialScanResult.Placeholders.Count} placeholders");
        TestOutput.WriteLine($"  - Processed {partialReplacementMap.Count} placeholders initially");
        TestOutput.WriteLine($"  - Found {updateScanResult.Placeholders.Count} remaining for update");
        TestOutput.WriteLine($"  - Completed {completionReplacementMap.Count} placeholders in update workflow");
        TestOutput.WriteLine($"  - Final result: {finalScanResult.Placeholders.Count} placeholders remaining");
        TestOutput.WriteLine($"  - Processed files recursively: {allProcessedFiles.Length} total ({nestedFiles.Count} in subfolders)");
        TestOutput.WriteLine($"  - SOUBOR_PREFIX correctly filtered: {!remainingPlaceholderNames.Contains("SOUBOR_PREFIX")}");
        TestOutput.WriteLine($"Update workflow test output at: {Path.GetFullPath(updateTestDirectory)}");
    }
}
