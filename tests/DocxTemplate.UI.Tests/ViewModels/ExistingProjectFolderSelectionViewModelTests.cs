using System.IO;
using DocxTemplate.UI.ViewModels;
using DocxTemplate.UI.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace DocxTemplate.UI.Tests.ViewModels;

public class ExistingProjectFolderSelectionViewModelTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // arrange & act
        var viewModel = new ExistingProjectFolderSelectionViewModel();

        // assert
        viewModel.SelectedFolderPath.Should().BeNull();
        viewModel.ValidationMessage.Should().Be("Prosím vyberte složku"); // Validation runs on construction
        viewModel.IsSelectingFolder.Should().BeFalse();
        viewModel.DocxFileCount.Should().Be(0);
        viewModel.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DocxFileCountText_WhenNoFiles_ShouldReturnCorrectText()
    {
        // arrange
        var viewModel = new ExistingProjectFolderSelectionViewModel();

        // act & assert
        viewModel.DocxFileCountText.Should().Be("Nenalezeny žádné .docx soubory");
    }

    [Fact]
    public void DocxFileCountText_WhenOneFile_ShouldReturnCorrectText()
    {
        // arrange
        var viewModel = new ExistingProjectFolderSelectionViewModel();
        
        // Use reflection to set the private field for testing
        var fieldInfo = typeof(ExistingProjectFolderSelectionViewModel)
            .GetField("_docxFileCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fieldInfo?.SetValue(viewModel, 1);

        // act & assert
        viewModel.DocxFileCountText.Should().Be("Nalezen 1 .docx soubor");
    }

    [Theory]
    [InlineData(2, "Nalezeno 2 .docx souborů")]
    [InlineData(5, "Nalezeno 5 .docx souborů")]
    [InlineData(10, "Nalezeno 10 .docx souborů")]
    public void DocxFileCountText_WhenMultipleFiles_ShouldReturnCorrectText(int fileCount, string expectedText)
    {
        // arrange
        var viewModel = new ExistingProjectFolderSelectionViewModel();
        
        // Use reflection to set the private field for testing
        var fieldInfo = typeof(ExistingProjectFolderSelectionViewModel)
            .GetField("_docxFileCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fieldInfo?.SetValue(viewModel, fileCount);

        // act & assert
        viewModel.DocxFileCountText.Should().Be(expectedText);
    }

    [Fact]
    public void ValidateStep_WhenNoFolderSelected_ShouldReturnFalse()
    {
        // arrange
        var viewModel = new ExistingProjectFolderSelectionViewModel();

        // act
        var result = viewModel.ValidateStep();

        // assert
        result.Should().BeFalse();
        viewModel.IsValid.Should().BeFalse();
        viewModel.ValidationMessage.Should().Be("Prosím vyberte složku");
    }

    [Fact]
    public void ValidateStep_WhenFolderDoesNotExist_ShouldReturnFalse()
    {
        // arrange
        var viewModel = new ExistingProjectFolderSelectionViewModel();
        viewModel.SelectedFolderPath = "/non/existent/folder";

        // act
        var result = viewModel.ValidateStep();

        // assert
        result.Should().BeFalse();
        viewModel.IsValid.Should().BeFalse();
        viewModel.ValidationMessage.Should().Be("Vybraná složka neexistuje");
    }

    [Fact]
    public void ValidateStep_WhenFolderExistsButNoDocxFiles_ShouldReturnFalse()
    {
        // arrange
        var tempDir = Path.GetTempPath();
        var testDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        
        try
        {
            var viewModel = new ExistingProjectFolderSelectionViewModel();
            viewModel.SelectedFolderPath = testDir;

            // act
            var result = viewModel.ValidateStep();

            // assert
            result.Should().BeFalse();
            viewModel.IsValid.Should().BeFalse();
            viewModel.ValidationMessage.Should().Be("Ve vybrané složce a jejích podsložkách nejsou žádné .docx soubory");
            viewModel.DocxFileCount.Should().Be(0);
        }
        finally
        {
            if (Directory.Exists(testDir))
                Directory.Delete(testDir, true);
        }
    }

    [Fact]
    public void ValidateStep_WhenFolderExistsWithDocxFiles_ShouldReturnTrue()
    {
        // arrange
        var tempDir = Path.GetTempPath();
        var testDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        
        try
        {
            // Create test .docx files
            File.WriteAllText(Path.Combine(testDir, "test1.docx"), "test content");
            File.WriteAllText(Path.Combine(testDir, "test2.docx"), "test content");
            
            var viewModel = new ExistingProjectFolderSelectionViewModel();
            viewModel.SelectedFolderPath = testDir;

            // act
            var result = viewModel.ValidateStep();

            // assert
            result.Should().BeTrue();
            viewModel.IsValid.Should().BeTrue();
            viewModel.ValidationMessage.Should().BeNull();
            viewModel.DocxFileCount.Should().Be(2);
        }
        finally
        {
            if (Directory.Exists(testDir))
                Directory.Delete(testDir, true);
        }
    }

    [Fact]
    public void CzechStrings_ShouldHaveCorrectValues()
    {
        // arrange
        var viewModel = new ExistingProjectFolderSelectionViewModel();

        // act & assert
        viewModel.FolderSelectionText.Should().Be("Vyberte výstupní složku");
        viewModel.FolderDescriptionText.Should().Be("Složka s částečně zpracovanými šablonami (včetně podsložek)");
    }
}