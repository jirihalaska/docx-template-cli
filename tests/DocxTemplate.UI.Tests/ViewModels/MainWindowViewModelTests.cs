using DocxTemplate.UI.ViewModels;

namespace DocxTemplate.UI.Tests.ViewModels;

public class MainWindowViewModelTests
{
    [Fact]
    public void MainWindowViewModel_ShouldInitializeWithDefaults()
    {
        // arrange & act
        var viewModel = new MainWindowViewModel();

        // assert
        Assert.Equal("Procesor šablon DOCX", viewModel.Title);
        Assert.Equal("Připraveno", viewModel.StatusText);
    }

    [Fact]
    public void MainWindowViewModel_ShouldUpdateTitleProperty()
    {
        // arrange
        var viewModel = new MainWindowViewModel();
        var newTitle = "New Title";

        // act
        viewModel.Title = newTitle;

        // assert
        Assert.Equal(newTitle, viewModel.Title);
    }

    [Fact]
    public void MainWindowViewModel_ShouldUpdateStatusTextProperty()
    {
        // arrange
        var viewModel = new MainWindowViewModel();
        var newStatus = "Zpracovává...";

        // act
        viewModel.StatusText = newStatus;

        // assert
        Assert.Equal(newStatus, viewModel.StatusText);
    }
}