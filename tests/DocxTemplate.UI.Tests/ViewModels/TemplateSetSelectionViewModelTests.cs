using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocxTemplate.UI.Services;
using DocxTemplate.UI.ViewModels;
using Moq;
using Xunit;

namespace DocxTemplate.UI.Tests.ViewModels;

/// <summary>
/// Unit tests for TemplateSetSelectionViewModel
/// </summary>
public class TemplateSetSelectionViewModelTests
{
    private readonly Mock<ITemplateSetDiscoveryService> _mockDiscoveryService;
    private readonly TemplateSetSelectionViewModel _viewModel;

    public TemplateSetSelectionViewModelTests()
    {
        _mockDiscoveryService = new Mock<ITemplateSetDiscoveryService>();
        _viewModel = new TemplateSetSelectionViewModel(_mockDiscoveryService.Object);
    }

    [Fact]
    public void Constructor_WithNullDiscoveryService_ThrowsArgumentNullException()
    {
        // arrange, act & assert
        Assert.Throws<ArgumentNullException>(() =>
            new TemplateSetSelectionViewModel(null!));
    }

    [Fact]
    public void InitialState_HasCorrectDefaults()
    {
        // arrange & act
        // Values set in constructor

        // assert
        Assert.Empty(_viewModel.TemplateSets);
        Assert.Null(_viewModel.SelectedTemplateSet);
        Assert.False(_viewModel.IsLoading);
        Assert.False(_viewModel.HasError);
        Assert.Empty(_viewModel.ErrorText);
        Assert.Equal("Vyberte sadu šablon", _viewModel.StepTitle);
        Assert.Equal("Vyberte sadu šablon pro zpracování z dostupných složek.", _viewModel.StepDescription);
    }

    [Fact]
    public void ValidateStep_WithNoSelection_ReturnsFalse()
    {
        // arrange
        // No template set selected (default state)

        // act
        var result = _viewModel.ValidateStep();

        // assert
        Assert.False(result);
        Assert.False(_viewModel.IsValid);
        Assert.Equal("Musíte vybrat sadu šablon pro pokračování.", _viewModel.ErrorMessage);
    }

    [Fact]
    public void ValidateStep_WithSelection_ReturnsTrue()
    {
        // arrange
        var templateSet = new TemplateSetItemViewModel(new TemplateSetInfo
        {
            Name = "Test Set",
            Path = "/path/to/test-set",
            FileCount = 5,
            TotalSize = 1200000,
            TotalSizeFormatted = "1.2 MB",
            LastModified = DateTime.UtcNow
        });
        _viewModel.SelectedTemplateSet = templateSet;

        // act
        var result = _viewModel.ValidateStep();

        // assert
        Assert.True(result);
        Assert.True(_viewModel.IsValid);
        Assert.Empty(_viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadTemplateSetsAsync_WithValidData_PopulatesTemplateSets()
    {
        // arrange
        var templateSetInfos = new List<TemplateSetInfo>
        {
            new() { Name = "Contracts", Path = "/path/to/contracts", FileCount = 15, TotalSize = 2300000, TotalSizeFormatted = "2.3 MB", LastModified = DateTime.UtcNow },
            new() { Name = "Reports", Path = "/path/to/reports", FileCount = 8, TotalSize = 1100000, TotalSizeFormatted = "1.1 MB", LastModified = DateTime.UtcNow }
        };

        _mockDiscoveryService
            .Setup(x => x.DiscoverTemplateSetsAsync("./templates", It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateSetInfos);

        // act
        await _viewModel.LoadTemplateSetsAsync();

        // assert
        Assert.False(_viewModel.IsLoading);
        Assert.False(_viewModel.HasError);
        Assert.Equal(2, _viewModel.TemplateSets.Count);

        var contracts = _viewModel.TemplateSets.First(ts => ts.Name == "Contracts");
        Assert.Equal(15, contracts.FileCount);
        Assert.Equal("Contracts (15 souborů)", contracts.DisplayText);
    }

    [Fact]
    public async Task LoadTemplateSetsAsync_WithNoData_ShowsErrorMessage()
    {
        // arrange
        var emptyList = new List<TemplateSetInfo>();

        _mockDiscoveryService
            .Setup(x => x.DiscoverTemplateSetsAsync("./templates", It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyList);

        // act
        await _viewModel.LoadTemplateSetsAsync();

        // assert
        Assert.False(_viewModel.IsLoading);
        Assert.True(_viewModel.HasError);
        Assert.Contains("Ve složce ./templates nebyly nalezeny žádné šablony", _viewModel.ErrorText);
        Assert.Empty(_viewModel.TemplateSets);
    }

    [Fact]
    public async Task LoadTemplateSetsAsync_WithException_ShowsErrorMessage()
    {
        // arrange
        const string errorMessage = "Network error";
        _mockDiscoveryService
            .Setup(x => x.DiscoverTemplateSetsAsync("./templates", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(errorMessage));

        // act
        await _viewModel.LoadTemplateSetsAsync();

        // assert
        Assert.False(_viewModel.IsLoading);
        Assert.True(_viewModel.HasError);
        Assert.Contains(errorMessage, _viewModel.ErrorText);
        Assert.Empty(_viewModel.TemplateSets);
    }

    [Fact]
    public void SelectTemplateSetCommand_DeselectsOthersAndSelectsChosen()
    {
        // arrange
        var templateSet1 = new TemplateSetItemViewModel(new TemplateSetInfo
        {
            Name = "Set 1",
            Path = "/path/to/set-1",
            FileCount = 5,
            TotalSize = 1000000,
            TotalSizeFormatted = "1.0 MB",
            LastModified = DateTime.UtcNow
        });
        var templateSet2 = new TemplateSetItemViewModel(new TemplateSetInfo
        {
            Name = "Set 2",
            Path = "/path/to/set-2",
            FileCount = 3,
            TotalSize = 500000,
            TotalSizeFormatted = "0.5 MB",
            LastModified = DateTime.UtcNow
        });

        _viewModel.TemplateSets.Add(templateSet1);
        _viewModel.TemplateSets.Add(templateSet2);

        // Initially select set 1
        templateSet1.IsSelected = true;

        // act
        _viewModel.SelectTemplateSetCommand.Execute(templateSet2).Subscribe();

        // assert
        Assert.False(templateSet1.IsSelected);
        Assert.True(templateSet2.IsSelected);
        Assert.Equal(templateSet2, _viewModel.SelectedTemplateSet);
    }

    [Fact]
    public void OnStepActivated_DoesNotThrow()
    {
        // arrange
        var templateSetInfos = new List<TemplateSetInfo>
        {
            new() { Name = "Test Set", Path = "/path/to/test-set", FileCount = 5, TotalSize = 1000000, TotalSizeFormatted = "1.0 MB", LastModified = DateTime.UtcNow }
        };

        _mockDiscoveryService
            .Setup(x => x.DiscoverTemplateSetsAsync("./templates", It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateSetInfos);

        // act & assert - just verify it doesn't throw
        // The actual loading logic is tested separately in LoadTemplateSetsAsync tests
        var exception = Record.Exception(() => _viewModel.OnStepActivated());
        Assert.Null(exception);
    }
}

/// <summary>
/// Unit tests for TemplateSetItemViewModel
/// </summary>
public class TemplateSetItemViewModelTests
{
    [Fact]
    public void Constructor_WithNullTemplateSetInfo_ThrowsArgumentNullException()
    {
        // arrange, act & assert
        Assert.Throws<ArgumentNullException>(() =>
            new TemplateSetItemViewModel(null!));
    }

    [Fact]
    public void Properties_ReturnCorrectValues()
    {
        // arrange
        var templateSetInfo = new TemplateSetInfo
        {
            Name = "Test Set",
            Path = "/path/to/test-set",
            FileCount = 10,
            TotalSize = 2500000,
            TotalSizeFormatted = "2.5 MB",
            LastModified = DateTime.UtcNow
        };

        // act
        var viewModel = new TemplateSetItemViewModel(templateSetInfo);

        // assert
        Assert.Equal("Test Set", viewModel.Name);
        Assert.Equal(10, viewModel.FileCount);
        Assert.Equal("Test Set (10 souborů)", viewModel.DisplayText);
        Assert.Equal("2.5 MB", viewModel.TotalSizeFormatted);
        Assert.False(viewModel.IsSelected);
    }

    [Fact]
    public void IsSelected_CanBeSetAndNotifies()
    {
        // arrange
        var templateSetInfo = new TemplateSetInfo
        {
            Name = "Test Set",
            Path = "/path/to/test-set",
            FileCount = 5,
            TotalSize = 1000000,
            TotalSizeFormatted = "1.0 MB",
            LastModified = DateTime.UtcNow
        };
        var viewModel = new TemplateSetItemViewModel(templateSetInfo);
        var propertyChanged = false;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(TemplateSetItemViewModel.IsSelected))
                propertyChanged = true;
        };

        // act
        viewModel.IsSelected = true;

        // assert
        Assert.True(viewModel.IsSelected);
        Assert.True(propertyChanged);
    }
}
