using System;
using System.Reflection;
using DocxTemplate.UI.Services;
using DocxTemplate.UI.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocxTemplate.UI.Tests.RegressionTests;

/// <summary>
/// Regression tests to ensure ViewModel constructors match their dependencies correctly
/// </summary>
public class ViewModelConstructorRegressionTests
{
    [Fact]
    public void MainWindowViewModel_Constructor_ShouldAcceptCorrectParameters()
    {
        // arrange - Get constructor parameters
        var constructorInfo = typeof(MainWindowViewModel).GetConstructors()[0];
        var parameters = constructorInfo.GetParameters();
        
        // act & assert - Verify expected constructor signature
        parameters.Should().HaveCount(1, "MainWindowViewModel should have exactly one constructor parameter");
        parameters[0].ParameterType.Should().Be(typeof(WizardViewModel),
            "MainWindowViewModel constructor should accept WizardViewModel");
    }

    [Fact]
    public void MainWindowViewModel_CanBeConstructedWithCorrectService()
    {
        // arrange - WizardViewModel is a concrete class and cannot be easily mocked
        // This test documents that MainWindowViewModel requires WizardViewModel (not a service interface)
        // In real usage, WizardViewModel is created by DI container with its own dependencies
        
        // act & assert - This test serves as documentation of the constructor signature
        var constructorInfo = typeof(MainWindowViewModel).GetConstructors()[0];
        var parameters = constructorInfo.GetParameters();
        
        parameters[0].ParameterType.Should().Be(typeof(WizardViewModel),
            "MainWindowViewModel requires WizardViewModel as constructor parameter");
    }

    [Fact]
    public void PlaceholderDiscoveryViewModel_Constructor_ShouldAcceptCorrectParameters()
    {
        // arrange - This ViewModel had incorrect mocking in tests
        var constructorInfo = typeof(PlaceholderDiscoveryViewModel).GetConstructors()[0];
        var parameters = constructorInfo.GetParameters();
        
        // act & assert - Verify the correct service type is expected
        parameters.Should().HaveCount(1, "PlaceholderDiscoveryViewModel should have exactly one constructor parameter");
        parameters[0].ParameterType.Should().Be(typeof(ICliCommandService),
            "PlaceholderDiscoveryViewModel constructor should accept ICliCommandService, not IPlaceholderScanService");
    }

    [Fact]
    public void PlaceholderDiscoveryViewModel_CanBeConstructedWithCorrectService()
    {
        // arrange - This was the source of test failures - wrong service type in mocks
        var mockCliCommandService = new Mock<ICliCommandService>();
        
        // act - Should not throw when constructing with correct parameter type
        Action construct = () => new PlaceholderDiscoveryViewModel(mockCliCommandService.Object);
        
        // assert
        construct.Should().NotThrow(
            "PlaceholderDiscoveryViewModel should construct successfully with ICliCommandService");
    }

    [Fact]
    public void ProcessingResultsViewModel_Constructor_ShouldAcceptCorrectParameters()
    {
        // arrange
        var constructorInfo = typeof(ProcessingResultsViewModel).GetConstructors()[0];
        var parameters = constructorInfo.GetParameters();
        
        // act & assert
        parameters.Should().HaveCount(1, "ProcessingResultsViewModel should have exactly one constructor parameter");
        parameters[0].ParameterType.Should().Be(typeof(ICliCommandService),
            "ProcessingResultsViewModel constructor should accept ICliCommandService");
    }

    [Fact]
    public void ProcessingResultsViewModel_CanBeConstructedWithCorrectService()
    {
        // arrange
        var mockCliCommandService = new Mock<ICliCommandService>();
        
        // act
        Action construct = () => new ProcessingResultsViewModel(mockCliCommandService.Object);
        
        // assert
        construct.Should().NotThrow(
            "ProcessingResultsViewModel should construct successfully with ICliCommandService");
    }

    [Fact]
    public void TemplateSetSelectionViewModel_Constructor_ShouldAcceptCorrectParameters()
    {
        // arrange
        var constructorInfo = typeof(TemplateSetSelectionViewModel).GetConstructors()[0];
        var parameters = constructorInfo.GetParameters();
        
        // act & assert
        parameters.Should().HaveCount(1, "TemplateSetSelectionViewModel should have exactly one constructor parameter");
        parameters[0].ParameterType.Should().Be(typeof(ITemplateSetDiscoveryService),
            "TemplateSetSelectionViewModel constructor should accept ITemplateSetDiscoveryService");
    }

    [Fact]
    public void TemplateSetSelectionViewModel_CanBeConstructedWithCorrectService()
    {
        // arrange
        var mockTemplateSetService = new Mock<ITemplateSetDiscoveryService>();
        
        // act
        Action construct = () => new TemplateSetSelectionViewModel(mockTemplateSetService.Object);
        
        // assert
        construct.Should().NotThrow(
            "TemplateSetSelectionViewModel should construct successfully with ITemplateSetDiscoveryService");
    }

    [Fact]
    public void AllViewModels_ShouldHaveNonNullChecksInConstructors()
    {
        // arrange - Get all ViewModel types
        var viewModelTypes = new[]
        {
            typeof(MainWindowViewModel),
            typeof(PlaceholderDiscoveryViewModel),
            typeof(ProcessingResultsViewModel),
            typeof(TemplateSetSelectionViewModel)
        };

        foreach (var viewModelType in viewModelTypes)
        {
            // arrange - Get constructor
            var constructor = viewModelType.GetConstructors()[0];
            var parameters = constructor.GetParameters();
            
            // act & assert - Should throw ArgumentNullException when passed null
            if (parameters.Length > 0)
            {
                var nullArgs = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    nullArgs[i] = null;
                }
                
                Action constructWithNull = () => Activator.CreateInstance(viewModelType, nullArgs);
                
                constructWithNull.Should().Throw<Exception>(
                    $"{viewModelType.Name} should validate constructor parameters and throw when passed null");
            }
        }
    }

    [Theory]
    [InlineData(typeof(MainWindowViewModel), typeof(WizardViewModel))]
    [InlineData(typeof(PlaceholderDiscoveryViewModel), typeof(ICliCommandService))]
    [InlineData(typeof(ProcessingResultsViewModel), typeof(ICliCommandService))]
    [InlineData(typeof(TemplateSetSelectionViewModel), typeof(ITemplateSetDiscoveryService))]
    public void ViewModelConstructor_ShouldMatchExpectedServiceType(Type viewModelType, Type expectedServiceType)
    {
        // arrange
        var constructor = viewModelType.GetConstructors()[0];
        var parameters = constructor.GetParameters();
        
        // act & assert - Verify constructor matches expected service dependency
        parameters.Should().HaveCount(1, $"{viewModelType.Name} should have exactly one dependency");
        parameters[0].ParameterType.Should().Be(expectedServiceType,
            $"{viewModelType.Name} should depend on {expectedServiceType.Name}");
    }

    [Fact]
    public void TestMocks_ShouldUseCorrectServiceTypes()
    {
        // arrange - This test documents the correct mock types to prevent future test failures
        // Focus on ViewModels that actually use service interfaces (not WizardViewModel)
        var correctMockTypes = new[]
        {
            (typeof(PlaceholderDiscoveryViewModel), typeof(ICliCommandService)), // This was wrong in original tests
            (typeof(ProcessingResultsViewModel), typeof(ICliCommandService)),
            (typeof(TemplateSetSelectionViewModel), typeof(ITemplateSetDiscoveryService))
        };

        foreach (var (viewModelType, serviceType) in correctMockTypes)
        {
            // act - Create mock of correct service type using direct Mock instantiation
            if (serviceType == typeof(ICliCommandService))
            {
                var mock = new Mock<ICliCommandService>();
                Action construct = () => Activator.CreateInstance(viewModelType, mock.Object);
                construct.Should().NotThrow(
                    $"Should be able to construct {viewModelType.Name} with mock {serviceType.Name}");
            }
            else if (serviceType == typeof(ITemplateSetDiscoveryService))
            {
                var mock = new Mock<ITemplateSetDiscoveryService>();
                Action construct = () => Activator.CreateInstance(viewModelType, mock.Object);
                construct.Should().NotThrow(
                    $"Should be able to construct {viewModelType.Name} with mock {serviceType.Name}");
            }
        }
    }
}