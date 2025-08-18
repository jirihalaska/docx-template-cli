using DocxTemplate.UI.ViewModels;
using DocxTemplate.UI.Services;
using DocxTemplate.Core.Services;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DocxTemplate.UI.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private WizardViewModel CreateMockWizardViewModel()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        
        // Mock all required ViewModels
        mockServiceProvider.Setup(sp => sp.GetService(typeof(TemplateSetSelectionViewModel)))
            .Returns(new Mock<TemplateSetSelectionViewModel>(Mock.Of<ITemplateSetDiscoveryService>()).Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(PlaceholderDiscoveryViewModel)))
            .Returns(new Mock<PlaceholderDiscoveryViewModel>(Mock.Of<ICliCommandService>()).Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(PlaceholderInputViewModel)))
            .Returns(new Mock<PlaceholderInputViewModel>().Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(OutputFolderSelectionViewModel)))
            .Returns(new Mock<OutputFolderSelectionViewModel>().Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ProcessingResultsViewModel)))
            .Returns(new Mock<ProcessingResultsViewModel>(Mock.Of<ICliCommandService>()).Object);
        
        return new WizardViewModel(mockServiceProvider.Object);
    }

    [Fact]
    public void MainWindowViewModel_ShouldInitializeWithDefaults()
    {
        // arrange & act
        var wizardViewModel = CreateMockWizardViewModel();
        var viewModel = new MainWindowViewModel(wizardViewModel);

        // assert
        Assert.Equal("Procesor šablon DOCX", viewModel.Title);
        Assert.Equal("Připraveno", viewModel.StatusText);
        Assert.NotNull(viewModel.WizardViewModel);
    }

    [Fact]
    public void MainWindowViewModel_ShouldUpdateTitleProperty()
    {
        // arrange
        var wizardViewModel = CreateMockWizardViewModel();
        var viewModel = new MainWindowViewModel(wizardViewModel);
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
        var wizardViewModel = CreateMockWizardViewModel();
        var viewModel = new MainWindowViewModel(wizardViewModel);
        var newStatus = "Zpracovává...";

        // act
        viewModel.StatusText = newStatus;

        // assert
        Assert.Equal(newStatus, viewModel.StatusText);
    }
}