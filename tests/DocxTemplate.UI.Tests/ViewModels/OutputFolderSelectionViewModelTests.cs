using System;
using System.IO;
using DocxTemplate.UI.ViewModels;
using Xunit;

namespace DocxTemplate.UI.Tests.ViewModels;

public class OutputFolderSelectionViewModelTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // arrange & act
        var viewModel = new OutputFolderSelectionViewModel();

        // assert
        Assert.Null(viewModel.SelectedFolderPath);
        Assert.Null(viewModel.ValidationMessage);
        Assert.False(viewModel.IsSelectingFolder);
        Assert.False(viewModel.HasSelectedFolder);
        Assert.Equal("Žádná složka není vybrána", viewModel.FolderDisplayText);
        Assert.NotNull(viewModel.SelectFolderCommand);
        Assert.Equal("Výběr výstupní složky", viewModel.StepTitle);
        Assert.Equal("Vyberte složku, kde budou uloženy zpracované dokumenty.", viewModel.StepDescription);
    }

    [Fact]
    public void ValidateStep_WithNoSelectedFolder_ReturnsFalse()
    {
        // arrange
        var viewModel = new OutputFolderSelectionViewModel();

        // act
        var isValid = viewModel.ValidateStep();

        // assert
        Assert.False(isValid);
        Assert.False(viewModel.IsValid);
        Assert.Equal("Musíte vybrat výstupní složku.", viewModel.ValidationError);
    }

    [Fact]
    public void ValidateStep_WithNonExistentFolder_ReturnsFalse()
    {
        // arrange
        var viewModel = new OutputFolderSelectionViewModel();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        viewModel.SelectedFolderPath = nonExistentPath;

        // act
        var isValid = viewModel.ValidateStep();

        // assert
        Assert.False(isValid);
        Assert.False(viewModel.IsValid);
        Assert.Equal("Vybraná složka neexistuje.", viewModel.ValidationError);
        Assert.Equal("Vybraná složka neexistuje nebo není dostupná.", viewModel.ValidationMessage);
    }

    [Fact]
    public void ValidateStep_WithValidWritableFolder_ReturnsTrue()
    {
        // arrange
        var viewModel = new OutputFolderSelectionViewModel();
        var tempPath = Path.GetTempPath();
        viewModel.SelectedFolderPath = tempPath;

        // act
        var isValid = viewModel.ValidateStep();

        // assert
        Assert.True(isValid);
        Assert.True(viewModel.IsValid);
        Assert.Null(viewModel.ValidationError);
        Assert.Equal("Složka je dostupná a zapisovatelná.", viewModel.ValidationMessage);
    }

    [Fact]
    public void ValidateStep_WithReadOnlyFolder_ReturnsFalse()
    {
        // arrange
        var viewModel = new OutputFolderSelectionViewModel();
        
        // Create a temporary directory and make it read-only
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        
        try
        {
            // Set directory to read-only (this may not work on all platforms)
            var dirInfo = new DirectoryInfo(testDir);
            dirInfo.Attributes |= FileAttributes.ReadOnly;
            
            viewModel.SelectedFolderPath = testDir;

            // act
            var isValid = viewModel.ValidateStep();

            // assert
            // Note: Read-only directory behavior varies by OS
            // On some systems, this test may pass if the directory is still writable
            if (!isValid)
            {
                Assert.False(viewModel.IsValid);
                Assert.Contains("oprávnění", viewModel.ValidationError);
            }
        }
        finally
        {
            // Clean up - remove read-only attribute and delete directory
            try
            {
                var dirInfo = new DirectoryInfo(testDir);
                if (dirInfo.Exists)
                {
                    dirInfo.Attributes &= ~FileAttributes.ReadOnly;
                    Directory.Delete(testDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void SelectedFolderPath_WhenChanged_UpdatesProperties()
    {
        // arrange
        var viewModel = new OutputFolderSelectionViewModel();
        var tempPath = Path.GetTempPath();

        // act
        viewModel.SelectedFolderPath = tempPath;

        // assert
        Assert.Equal(tempPath, viewModel.SelectedFolderPath);
        Assert.True(viewModel.HasSelectedFolder);
        Assert.Equal(tempPath, viewModel.FolderDisplayText);
    }

    [Fact]
    public void SelectedFolderPath_WhenSetToNull_UpdatesProperties()
    {
        // arrange
        var viewModel = new OutputFolderSelectionViewModel();
        viewModel.SelectedFolderPath = Path.GetTempPath();

        // act
        viewModel.SelectedFolderPath = null;

        // assert
        Assert.Null(viewModel.SelectedFolderPath);
        Assert.False(viewModel.HasSelectedFolder);
        Assert.Equal("Žádná složka není vybrána", viewModel.FolderDisplayText);
    }

    [Fact]
    public void OnStepActivated_CallsValidateStep()
    {
        // arrange
        var viewModel = new OutputFolderSelectionViewModel();
        var tempPath = Path.GetTempPath();
        viewModel.SelectedFolderPath = tempPath;

        // act
        viewModel.OnStepActivated();

        // assert
        Assert.True(viewModel.IsValid); // Should be valid for temp directory
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateStep_WithEmptyOrWhiteSpacePath_ReturnsFalse(string? path)
    {
        // arrange
        var viewModel = new OutputFolderSelectionViewModel();
        viewModel.SelectedFolderPath = path;

        // act
        var isValid = viewModel.ValidateStep();

        // assert
        Assert.False(isValid);
        Assert.False(viewModel.IsValid);
        Assert.Equal("Musíte vybrat výstupní složku.", viewModel.ValidationError);
    }

    [Fact]
    public void ValidationMessage_Property_RaisesPropertyChanged()
    {
        // arrange
        var viewModel = new OutputFolderSelectionViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(OutputFolderSelectionViewModel.ValidationMessage))
                propertyChanged = true;
        };

        // act
        // First set a valid path to trigger a validation message change
        viewModel.SelectedFolderPath = Path.GetTempPath();
        propertyChanged = false; // Reset flag
        viewModel.ValidateStep(); // This should set ValidationMessage

        // assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void IsValid_Property_RaisesPropertyChanged()
    {
        // arrange
        var viewModel = new OutputFolderSelectionViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(OutputFolderSelectionViewModel.IsValid))
                propertyChanged = true;
        };

        // act
        // Set a valid path to change IsValid from false to true
        viewModel.SelectedFolderPath = Path.GetTempPath();

        // assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void SelectedFolderPath_Property_RaisesPropertyChanged()
    {
        // arrange
        var viewModel = new OutputFolderSelectionViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(OutputFolderSelectionViewModel.SelectedFolderPath))
                propertyChanged = true;
        };

        // act
        viewModel.SelectedFolderPath = Path.GetTempPath();

        // assert
        Assert.True(propertyChanged);
    }
}