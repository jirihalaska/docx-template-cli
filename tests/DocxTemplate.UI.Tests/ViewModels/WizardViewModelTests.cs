using System;
using System.Reactive.Linq;
using DocxTemplate.UI.ViewModels;
using Xunit;
using FluentAssertions;

namespace DocxTemplate.UI.Tests.ViewModels;

public class WizardViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize_WithCorrectDefaults()
    {
        // arrange & act
        var viewModel = new WizardViewModel();

        // assert
        viewModel.CurrentStep.Should().Be(1);
        viewModel.Steps.Should().HaveCount(5);
        viewModel.Steps[0].Title.Should().Be("Vyberte sadu šablon");
        viewModel.Steps[0].IsActive.Should().BeTrue();
        viewModel.StepIndicatorText.Should().Be("Krok 1 z 5");
        viewModel.CanAdvanceToNextStep.Should().BeTrue();
    }

    [Fact]
    public void Steps_ShouldHave_CorrectCzechTitles()
    {
        // arrange & act
        var viewModel = new WizardViewModel();

        // assert
        viewModel.Steps[0].Title.Should().Be("Vyberte sadu šablon");
        viewModel.Steps[1].Title.Should().Be("Nalezení zástupných symbolů");
        viewModel.Steps[2].Title.Should().Be("Zadání hodnot zástupných symbolů");
        viewModel.Steps[3].Title.Should().Be("Výběr výstupní složky");
        viewModel.Steps[4].Title.Should().Be("Zpracování a výsledky");
    }

    [Fact]
    public void NextCommand_WhenCanAdvanceToNextStep_ShouldBeEnabled()
    {
        // arrange
        var viewModel = new WizardViewModel();

        // act & assert
        viewModel.NextCommand.CanExecute.FirstAsync().Wait().Should().BeTrue();
    }

    [Fact]
    public void NextCommand_WhenAtLastStep_ShouldBeDisabled()
    {
        // arrange
        var viewModel = new WizardViewModel();
        
        // act - navigate to last step
        for (int i = 1; i < 5; i++)
        {
            viewModel.NextCommand.Execute().Subscribe();
        }

        // assert
        viewModel.CurrentStep.Should().Be(5);
        viewModel.NextCommand.CanExecute.FirstAsync().Wait().Should().BeFalse();
    }

    [Fact]
    public void BackCommand_WhenAtFirstStep_ShouldBeDisabled()
    {
        // arrange
        var viewModel = new WizardViewModel();

        // act & assert
        viewModel.CurrentStep.Should().Be(1);
        viewModel.BackCommand.CanExecute.FirstAsync().Wait().Should().BeFalse();
    }

    [Fact]
    public void BackCommand_WhenNotAtFirstStep_ShouldBeEnabled()
    {
        // arrange
        var viewModel = new WizardViewModel();
        
        // act
        viewModel.NextCommand.Execute().Subscribe();

        // assert
        viewModel.CurrentStep.Should().Be(2);
        viewModel.BackCommand.CanExecute.FirstAsync().Wait().Should().BeTrue();
    }

    [Fact]
    public void NextCommand_Execute_ShouldAdvanceStepAndMarkPreviousAsCompleted()
    {
        // arrange
        var viewModel = new WizardViewModel();

        // act
        viewModel.NextCommand.Execute().Subscribe();

        // assert
        viewModel.CurrentStep.Should().Be(2);
        viewModel.Steps[0].IsCompleted.Should().BeTrue();
        viewModel.Steps[1].IsActive.Should().BeTrue();
        viewModel.StepIndicatorText.Should().Be("Krok 2 z 5");
    }

    [Fact]
    public void BackCommand_Execute_ShouldGoToPreviousStep()
    {
        // arrange
        var viewModel = new WizardViewModel();
        viewModel.NextCommand.Execute().Subscribe();

        // act
        viewModel.BackCommand.Execute().Subscribe();

        // assert
        viewModel.CurrentStep.Should().Be(1);
        viewModel.Steps[0].IsActive.Should().BeTrue();
        viewModel.Steps[1].IsActive.Should().BeFalse();
        viewModel.StepIndicatorText.Should().Be("Krok 1 z 5");
    }

    [Fact]
    public void CurrentStepTitle_ShouldReturnCorrectTitle_ForCurrentStep()
    {
        // arrange
        var viewModel = new WizardViewModel();

        // act & assert
        viewModel.CurrentStepTitle.Should().Be("Vyberte sadu šablon");
        
        viewModel.NextCommand.Execute().Subscribe();
        viewModel.CurrentStepTitle.Should().Be("Nalezení zástupných symbolů");
    }

    [Fact]
    public void NextCommand_WhenCanAdvanceToNextStepIsFalse_ShouldBeDisabled()
    {
        // arrange
        var viewModel = new WizardViewModel();
        viewModel.CanAdvanceToNextStep = false;

        // act & assert
        viewModel.NextCommand.CanExecute.FirstAsync().Wait().Should().BeFalse();
    }

    [Fact]
    public void CurrentStepContent_ShouldReturnCorrectView_ForCurrentStep()
    {
        // arrange
        var viewModel = new WizardViewModel();

        // act & assert
        viewModel.CurrentStepContent.Should().NotBeNull();
        viewModel.CurrentStepContent.GetType().Name.Should().Be("Step1TemplateSelectionView");

        viewModel.NextCommand.Execute().Subscribe();
        viewModel.CurrentStepContent.GetType().Name.Should().Be("Step2PlaceholderDiscoveryView");
    }

    [Fact]
    public void UpdateStepStates_ShouldSetOnlyCurrentStepAsActive()
    {
        // arrange
        var viewModel = new WizardViewModel();

        // act
        viewModel.NextCommand.Execute().Subscribe();

        // assert
        viewModel.Steps[0].IsActive.Should().BeFalse();
        viewModel.Steps[1].IsActive.Should().BeTrue();
        viewModel.Steps[2].IsActive.Should().BeFalse();
        viewModel.Steps[3].IsActive.Should().BeFalse();
        viewModel.Steps[4].IsActive.Should().BeFalse();
    }
}