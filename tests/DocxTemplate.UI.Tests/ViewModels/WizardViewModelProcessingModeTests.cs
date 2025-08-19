using DocxTemplate.UI.Models;
using DocxTemplate.UI.ViewModels;
using DocxTemplate.UI.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace DocxTemplate.UI.Tests.ViewModels;

public class WizardViewModelProcessingModeTests
{
    [Fact]
    public void ProcessingMode_DefaultValue_ShouldBeNewProject()
    {
        // arrange
        var serviceProvider = TestServiceProviderFactory.Create();

        // act
        var viewModel = new WizardViewModel(serviceProvider);

        // assert
        viewModel.SelectedMode.Should().Be(ProcessingMode.NewProject);
    }

    [Theory]
    [InlineData(ProcessingMode.NewProject, 5)]
    [InlineData(ProcessingMode.UpdateProject, 5)]
    public void TotalSteps_ShouldReturnCorrectValue_BasedOnSelectedMode(ProcessingMode mode, int expectedSteps)
    {
        // arrange
        var serviceProvider = TestServiceProviderFactory.Create();
        var viewModel = new WizardViewModel(serviceProvider);

        // act
        viewModel.SelectedMode = mode;

        // assert
        viewModel.TotalSteps.Should().Be(expectedSteps);
    }

    [Theory]
    [InlineData(ProcessingMode.NewProject, 1, "Krok 1 z 4")]
    [InlineData(ProcessingMode.NewProject, 3, "Krok 3 z 4")]
    [InlineData(ProcessingMode.UpdateProject, 1, "Krok 1 z 4")]
    [InlineData(ProcessingMode.UpdateProject, 4, "Krok 4 z 4")]
    public void StepIndicatorText_ShouldShowCorrectText_BasedOnModeAndCurrentStep(
        ProcessingMode mode, int currentStep, string expectedText)
    {
        // arrange
        var serviceProvider = TestServiceProviderFactory.Create();
        var viewModel = new WizardViewModel(serviceProvider);

        // act
        viewModel.SelectedMode = mode;
        viewModel.CurrentStep = currentStep;

        // assert
        viewModel.StepIndicatorText.Should().Be(expectedText);
    }

    [Fact]
    public void SelectedMode_WhenChanged_ShouldRaisePropertyChangedForTotalSteps()
    {
        // arrange
        var serviceProvider = TestServiceProviderFactory.Create();
        var viewModel = new WizardViewModel(serviceProvider);
        var totalStepsChanged = false;
        var stepIndicatorChanged = false;

        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(viewModel.TotalSteps))
                totalStepsChanged = true;
            if (args.PropertyName == nameof(viewModel.StepIndicatorText))
                stepIndicatorChanged = true;
        };

        // act
        viewModel.SelectedMode = ProcessingMode.UpdateProject;

        // assert
        totalStepsChanged.Should().BeTrue();
        stepIndicatorChanged.Should().BeTrue();
    }
}