using System;
using System.Reflection;
using DocxTemplate.UI.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocxTemplate.UI.Tests.RegressionTests;

/// <summary>
/// Regression tests to ensure CliCommandBuilder methods remain virtual for mocking
/// </summary>
public class CliCommandBuilderRegressionTests
{
    [Fact]
    public void CliCommandBuilder_AllPublicMethods_ShouldBeVirtualForMocking()
    {
        // arrange - Get all public methods that should be mockable
        var type = typeof(CliCommandBuilder);
        var publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        
        // act & assert - All public methods should be virtual to enable Moq proxying
        foreach (var method in publicMethods)
        {
            // Skip special methods like property getters/setters and constructors
            if (method.IsSpecialName || method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                continue;
                
            method.IsVirtual.Should().BeTrue(
                $"Method {method.Name} should be virtual to enable mocking with Moq. " +
                "This was the root cause of TemplateSetDiscoveryServiceTests failures.");
        }
    }

    [Fact]
    public void CliCommandBuilder_BuildListSetsCommand_ShouldBeVirtual()
    {
        // arrange - This specific method was causing test failures
        var type = typeof(CliCommandBuilder);
        var method = type.GetMethod(nameof(CliCommandBuilder.BuildListSetsCommand));
        
        // act & assert
        method.Should().NotBeNull("BuildListSetsCommand method should exist");
        method!.IsVirtual.Should().BeTrue(
            "BuildListSetsCommand must be virtual for TemplateSetDiscoveryService mocking");
    }

    [Fact]
    public void CliCommandBuilder_BuildScanCommand_ShouldBeVirtual()
    {
        // arrange
        var type = typeof(CliCommandBuilder);
        var method = type.GetMethod(nameof(CliCommandBuilder.BuildScanCommand));
        
        // act & assert
        method.Should().NotBeNull("BuildScanCommand method should exist");
        method!.IsVirtual.Should().BeTrue(
            "BuildScanCommand must be virtual for placeholder discovery mocking");
    }

    [Fact]
    public void CliCommandBuilder_BuildCopyCommand_ShouldBeVirtual()
    {
        // arrange
        var type = typeof(CliCommandBuilder);
        var method = type.GetMethod(nameof(CliCommandBuilder.BuildCopyCommand));
        
        // act & assert
        method.Should().NotBeNull("BuildCopyCommand method should exist");
        method!.IsVirtual.Should().BeTrue(
            "BuildCopyCommand must be virtual for template copying mocking");
    }

    [Fact]
    public void CliCommandBuilder_BuildReplaceCommand_ShouldBeVirtual()
    {
        // arrange
        var type = typeof(CliCommandBuilder);
        var method = type.GetMethod(nameof(CliCommandBuilder.BuildReplaceCommand));
        
        // act & assert
        method.Should().NotBeNull("BuildReplaceCommand method should exist");
        method!.IsVirtual.Should().BeTrue(
            "BuildReplaceCommand must be virtual for placeholder replacement mocking");
    }

    [Fact]
    public void CliCommandBuilder_CanBeSuccessfullyMockedWithMoq()
    {
        // arrange - This test verifies that Moq can actually create a proxy
        // This was failing before methods were made virtual
        
        // act - Should not throw when creating mock
        Action createMock = () =>
        {
            var mock = new Mock<CliCommandBuilder>();
            
            // Setup some method calls to verify mocking works
            mock.Setup(x => x.BuildListSetsCommand(It.IsAny<string>()))
                .Returns(new CliCommandBuilder.CliCommand("test", new[] { "arg1" }));
                
            mock.Setup(x => x.BuildScanCommand(It.IsAny<string>()))
                .Returns(new CliCommandBuilder.CliCommand("scan", new[] { "arg1" }));
        };
        
        // assert - Should not throw Castle.DynamicProxy exceptions
        createMock.Should().NotThrow(
            "CliCommandBuilder should be mockable with Moq when methods are virtual");
    }

    [Fact]
    public void CliCommandBuilder_MockedMethods_ShouldReturnConfiguredValues()
    {
        // arrange - Test that mocked methods actually work as expected
        var mock = new Mock<CliCommandBuilder>();
        var expectedCommand = new CliCommandBuilder.CliCommand("mocked", new[] { "--test", "value" });
        
        mock.Setup(x => x.BuildListSetsCommand("/test/path"))
            .Returns(expectedCommand);
        
        // act
        var result = mock.Object.BuildListSetsCommand("/test/path");
        
        // assert
        result.Should().BeEquivalentTo(expectedCommand, 
            "Mocked CliCommandBuilder should return configured command objects");
    }

    [Fact]
    public void CliCommandBuilder_RealImplementation_ShouldStillWork()
    {
        // arrange - Ensure making methods virtual doesn't break real usage
        var builder = new CliCommandBuilder();
        
        // act - Test real method calls work correctly
        var listCommand = builder.BuildListSetsCommand("/test/templates");
        var scanCommand = builder.BuildScanCommand("/test/path");
        var copyCommand = builder.BuildCopyCommand("/source", "/target");
        var replaceCommand = builder.BuildReplaceCommand("/folder", "/map.json");
        
        // assert - Real implementation should work normally
        listCommand.CommandName.Should().Be("list-sets");
        listCommand.Arguments.Should().Contain("--templates");
        
        scanCommand.CommandName.Should().Be("scan");
        scanCommand.Arguments.Should().Contain("--path");
        
        copyCommand.CommandName.Should().Be("copy");
        copyCommand.Arguments.Should().Contain("--source");
        copyCommand.Arguments.Should().Contain("--target");
        
        replaceCommand.CommandName.Should().Be("replace");
        replaceCommand.Arguments.Should().Contain("--folder");
        replaceCommand.Arguments.Should().Contain("--map");
    }
}